﻿using NUnit.Framework;
using Ryujinx.Memory.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Memory.Tests
{
    public class MultiRegionTrackingTests
    {
        private const int RndCnt = 3;

        private const ulong MemorySize = 0x8000;
        private const int PageSize = 4096;

        private MemoryBlock _memoryBlock;
        private MemoryTracking _tracking;
        private MockVirtualMemoryManager _memoryManager;

        [SetUp]
        public void Setup()
        {
            _memoryBlock = new MemoryBlock(MemorySize);
            _memoryManager = new MockVirtualMemoryManager(MemorySize, PageSize);
            _tracking = new MemoryTracking(_memoryManager, PageSize);
        }

        [TearDown]
        public void Teardown()
        {
            _memoryBlock.Dispose();
        }

        private IMultiRegionHandle GetGranular(bool smart, ulong address, ulong size, ulong granularity)
        {
            return smart ?
                _tracking.BeginSmartGranularTracking(address, size, granularity) :
                (IMultiRegionHandle)_tracking.BeginGranularTracking(address, size, null, granularity);
        }

        private void RandomOrder(Random random, List<int> indices, Action<int> action)
        {
            List<int> choices = indices.ToList();

            while (choices.Count > 0)
            {
                int choice = random.Next(choices.Count);
                action(choices[choice]);
                choices.RemoveAt(choice);
            }
        }

        private int ExpectQueryInOrder(IMultiRegionHandle handle, ulong startAddress, ulong size, Func<ulong, bool> addressPredicate)
        {
            int regionCount = 0;
            ulong lastAddress = startAddress;

            handle.QueryModified(startAddress, size, (address, range) =>
            {
                Assert.IsTrue(addressPredicate(address)); // Written pages must be even.
                Assert.GreaterOrEqual(address, lastAddress); // Must be signalled in ascending order, regardless of write order.
                lastAddress = address;
                regionCount++;
            });

            return regionCount;
        }

        private int ExpectQueryInOrder(IMultiRegionHandle handle, ulong startAddress, ulong size, Func<ulong, bool> addressPredicate, int sequenceNumber)
        {
            int regionCount = 0;
            ulong lastAddress = startAddress;

            handle.QueryModified(startAddress, size, (address, range) =>
            {
                Assert.IsTrue(addressPredicate(address)); // Written pages must be even.
                Assert.GreaterOrEqual(address, lastAddress); // Must be signalled in ascending order, regardless of write order.
                lastAddress = address;
                regionCount++;
            }, sequenceNumber);

            return regionCount;
        }

        private void PreparePages(IMultiRegionHandle handle, int pageCount, ulong address = 0)
        {
            Random random = new Random();

            // Make sure the list has minimum granularity (smart region changes granularity based on requested ranges)
            RandomOrder(random, Enumerable.Range(0, pageCount).ToList(), (i) =>
            {
                ulong resultAddress = ulong.MaxValue;
                handle.QueryModified((ulong)i * PageSize + address, PageSize, (address, range) =>
                {
                    resultAddress = address;
                });
                Assert.AreEqual(resultAddress, (ulong)i * PageSize + address);
            });
        }

        [Test]
        public void DirtyRegionOrdering([Values] bool smart)
        {
            const int pageCount = 32;
            IMultiRegionHandle handle = GetGranular(smart, 0, PageSize * pageCount, PageSize);

            Random random = new Random();

            PreparePages(handle, pageCount);

            IEnumerable<int> halfRange = Enumerable.Range(0, pageCount / 2);
            List<int> odd = halfRange.Select(x => x * 2 + 1).ToList();
            List<int> even = halfRange.Select(x => x * 2).ToList();

            // Write to all the odd pages.
            RandomOrder(random, odd, (i) =>
            {
                _tracking.VirtualMemoryEvent((ulong)i * PageSize, PageSize, true);
            });

            int oddRegionCount = ExpectQueryInOrder(handle, 0, PageSize * pageCount, (address) => (address / PageSize) % 2 == 1);

            Assert.AreEqual(oddRegionCount, pageCount / 2); // Must have written to all odd pages.

            // Write to all the even pages.
            RandomOrder(random, even, (i) =>
            {
                _tracking.VirtualMemoryEvent((ulong)i * PageSize, PageSize, true);
            });

            int evenRegionCount = ExpectQueryInOrder(handle, 0, PageSize * pageCount, (address) => (address / PageSize) % 2 == 0);

            Assert.AreEqual(evenRegionCount, pageCount / 2);
        }

        [Test]
        public void SequenceNumber([Values] bool smart)
        {
            // The sequence number can be used to ignore dirty flags, and defer their consumption until later.
            // If a user consumes a dirty flag with sequence number 1, then there is a write to the protected region,
            // the dirty flag will not be acknowledged until the sequence number is 2.

            // This is useful for situations where we know that the data was complete when the sequence number was set.
            // ...essentially, when that data can only be updated on a future sequence number.

            const int pageCount = 32;
            IMultiRegionHandle handle = GetGranular(smart, 0, PageSize * pageCount, PageSize);

            PreparePages(handle, pageCount);

            Random random = new Random();

            IEnumerable<int> halfRange = Enumerable.Range(0, pageCount / 2);
            List<int> odd = halfRange.Select(x => x * 2 + 1).ToList();
            List<int> even = halfRange.Select(x => x * 2).ToList();

            // Write to all the odd pages.
            RandomOrder(random, odd, (i) =>
            {
                _tracking.VirtualMemoryEvent((ulong)i * PageSize, PageSize, true);
            });

            int oddRegionCount = 0;

            // Track with sequence number 1. Future dirty flags should only be consumed with sequence number != 1.
            // Only track the odd pages, so the even ones don't have their sequence number set.

            foreach (int index in odd)
            {
                handle.QueryModified((ulong)index * PageSize, PageSize, (address, range) =>
                {
                    oddRegionCount++;
                }, 1);
            }

            Assert.AreEqual(oddRegionCount, pageCount / 2); // Must have written to all odd pages.

            // Write to all pages.

            _tracking.VirtualMemoryEvent(0, PageSize * pageCount, true);

            // Only the even regions should be reported for sequence number 1.

            int evenRegionCount = ExpectQueryInOrder(handle, 0, PageSize * pageCount, (address) => (address / PageSize) % 2 == 0, 1);

            Assert.AreEqual(evenRegionCount, pageCount / 2); // Must have written to all even pages.

            oddRegionCount = 0;

            handle.QueryModified(0, PageSize * pageCount, (address, range) => { oddRegionCount++; }, 1);

            Assert.AreEqual(oddRegionCount, 0); // Sequence number has not changed, so found no dirty subregions.

            // With sequence number 2, all all pages should be reported as modified.

            oddRegionCount = ExpectQueryInOrder(handle, 0, PageSize * pageCount, (address) => (address / PageSize) % 2 == 1, 2);

            Assert.AreEqual(oddRegionCount, pageCount / 2); // Must have written to all odd pages.
        }

        [Test]
        public void SmartRegionTracking()
        {
            // Smart multi region handles dynamically change their tracking granularity based on QueryMemory calls.
            // This can save on reprotects on larger resources.

            const int pageCount = 32;
            IMultiRegionHandle handle = GetGranular(true, 0, PageSize * pageCount, PageSize);

            // Query some large regions to prep the subdivision of the tracking region.

            int[] regionSizes = new int[] { 6, 4, 3, 2, 6, 1 };
            ulong address = 0;

            for (int i = 0; i < regionSizes.Length; i++)
            {
                int region = regionSizes[i];
                handle.QueryModified(address, (ulong)(PageSize * region), (address, size) => { });
                
                // There should be a gap between regions,
                // So that they don't combine and we can see the full effects.
                address += (ulong)(PageSize * (region + 1));
            }

            // Clear modified.
            handle.QueryModified((address, size) => { });

            // Trigger each region with a 1 byte write.
            address = 0;

            for (int i = 0; i < regionSizes.Length; i++)
            {
                int region = regionSizes[i];
                _tracking.VirtualMemoryEvent(address, 1, true);
                address += (ulong)(PageSize * (region + 1));
            }

            int regionInd = 0;
            ulong expectedAddress = 0;

            // Expect each region to trigger in its entirety, in address ascending order.
            handle.QueryModified((address, size) => {
                int region = regionSizes[regionInd++];

                Assert.AreEqual(address, expectedAddress);
                Assert.AreEqual(size, (ulong)(PageSize * region));

                expectedAddress += (ulong)(PageSize * (region + 1));
            });
        }

        [Test]
        public void DisposeMultiHandles([Values] bool smart)
        {
            // Create and initialize two overlapping Multi Region Handles, with PageSize granularity.
            const int pageCount = 32;
            const int overlapStart = 16;

            Assert.AreEqual(0, _tracking.GetRegionCount());

            IMultiRegionHandle handleLow = GetGranular(smart, 0, PageSize * pageCount, PageSize);
            PreparePages(handleLow, pageCount);

            Assert.AreEqual(pageCount, _tracking.GetRegionCount());

            IMultiRegionHandle handleHigh = GetGranular(smart, PageSize * overlapStart, PageSize * pageCount, PageSize);
            PreparePages(handleHigh, pageCount, PageSize * overlapStart);

            // Combined pages (and assuming overlapStart <= pageCount) should be pageCount after overlapStart.
            int totalPages = overlapStart + pageCount;

            Assert.AreEqual(totalPages, _tracking.GetRegionCount());

            handleLow.Dispose(); // After disposing one, the pages for the other remain.

            Assert.AreEqual(pageCount, _tracking.GetRegionCount());

            handleHigh.Dispose(); // After disposing the other, there are no pages left.

            Assert.AreEqual(0, _tracking.GetRegionCount());
        }

        [Test]
        public void InheritHandles()
        {
            // Test merging the following into a granular region handle:
            // - 3x gap (creates new granular handles)
            // - 3x from multiregion: not dirty, dirty and with action
            // - 2x gap
            // - 3x single page: not dirty, dirty and with action
            // - 3x two page: not dirty, dirty and with action (handle is not reused, but its state is copied to the granular handles)
            // - 1x gap
            // For a total of 18 pages.

            bool[] actionsTriggered = new bool[3];

            MultiRegionHandle granular = _tracking.BeginGranularTracking(PageSize * 3, PageSize * 3, null, PageSize);
            PreparePages(granular, 3, PageSize * 3);

            // Write to the second handle in the multiregion.
            _tracking.VirtualMemoryEvent(PageSize * 4, PageSize, true);

            // Add an action to the third handle in the multiregion.
            granular.RegisterAction(PageSize * 5, PageSize, (_, _) => { actionsTriggered[0] = true; });

            RegionHandle[] singlePages = new RegionHandle[3];

            for (int i = 0; i < 3; i++)
            {
                singlePages[i] = _tracking.BeginTracking(PageSize * (8 + (ulong)i), PageSize);
                singlePages[i].Reprotect();
            }

            // Write to the second handle.
            _tracking.VirtualMemoryEvent(PageSize * 9, PageSize, true);

            // Add an action to the third handle.
            singlePages[2].RegisterAction((_, _) => { actionsTriggered[1] = true; });

            RegionHandle[] doublePages = new RegionHandle[3];

            for (int i = 0; i < 3; i++)
            {
                doublePages[i] = _tracking.BeginTracking(PageSize * (11 + (ulong)i * 2), PageSize * 2);
                doublePages[i].Reprotect();
            }

            // Write to the second handle.
            _tracking.VirtualMemoryEvent(PageSize * 13, PageSize * 2, true);

            // Add an action to the third handle.
            doublePages[2].RegisterAction((_, _) => { actionsTriggered[2] = true; });

            // Finally, create a granular handle that inherits all these handles.

            IEnumerable<IRegionHandle>[] handleGroups = new IEnumerable<IRegionHandle>[]
            {
                granular.GetHandles(),
                singlePages,
                doublePages
            };

            MultiRegionHandle combined = _tracking.BeginGranularTracking(0, PageSize * 18, handleGroups.SelectMany((handles) => handles), PageSize);

            bool[] expectedDirty = new bool[]
            {
                true, true, true, // Gap.
                false, true, false, // Multi-region.
                true, true, // Gap.
                false, true, false, // Individual handles.
                false, false, true, true, false, false, // Double size handles.
                true // Gap.
            };

            for (int i = 0; i < 18; i++)
            {
                bool modified = false;
                combined.QueryModified(PageSize * (ulong)i, PageSize, (_, _) => { modified = true; });

                Assert.AreEqual(expectedDirty[i], modified);
            }

            Assert.AreEqual(new bool[3], actionsTriggered);

            _tracking.VirtualMemoryEvent(PageSize * 5, PageSize, false);
            Assert.IsTrue(actionsTriggered[0]);

            _tracking.VirtualMemoryEvent(PageSize * 10, PageSize, false);
            Assert.IsTrue(actionsTriggered[1]);

            _tracking.VirtualMemoryEvent(PageSize * 15, PageSize, false);
            Assert.IsTrue(actionsTriggered[2]);

            // The double page handles should be disposed, as they were split into granular handles.
            foreach (RegionHandle doublePage in doublePages)
            {
                // These should have been disposed.
                bool throws = false;

                try
                {
                    doublePage.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    throws = true;
                }

                Assert.IsTrue(throws);
            }

            IEnumerable<IRegionHandle> combinedHandles = combined.GetHandles();

            Assert.AreEqual(handleGroups[0].ElementAt(0), combinedHandles.ElementAt(3));
            Assert.AreEqual(handleGroups[0].ElementAt(1), combinedHandles.ElementAt(4));
            Assert.AreEqual(handleGroups[0].ElementAt(2), combinedHandles.ElementAt(5));

            Assert.AreEqual(singlePages[0], combinedHandles.ElementAt(8));
            Assert.AreEqual(singlePages[1], combinedHandles.ElementAt(9));
            Assert.AreEqual(singlePages[2], combinedHandles.ElementAt(10));
        }

        [Test]
        public void PreciseAction()
        {
            bool actionTriggered = false;

            MultiRegionHandle granular = _tracking.BeginGranularTracking(PageSize * 3, PageSize * 3, null, PageSize);
            PreparePages(granular, 3, PageSize * 3);

            // Add a precise action to the second and third handle in the multiregion.
            granular.RegisterPreciseAction(PageSize * 4, PageSize * 2, (_, _, _) => { actionTriggered = true; return true; });

            // Precise write to first handle in the multiregion.
            _tracking.VirtualMemoryEvent(PageSize * 3, PageSize, true, precise: true);
            Assert.IsFalse(actionTriggered); // Action not triggered.

            bool firstPageModified = false;
            granular.QueryModified(PageSize * 3, PageSize, (_, _) => { firstPageModified = true; });
            Assert.IsTrue(firstPageModified); // First page is modified.

            // Precise write to all handles in the multiregion.
            _tracking.VirtualMemoryEvent(PageSize * 3, PageSize * 3, true, precise: true);

            bool[] pagesModified = new bool[3];

            for (int i = 3; i < 6; i++)
            {
                int index = i - 3;
                granular.QueryModified(PageSize * (ulong)i, PageSize, (_, _) => { pagesModified[index] = true; });
            }

            Assert.IsTrue(actionTriggered); // Action triggered.

            // Precise writes are ignored on two later handles due to the action returning true.
            Assert.AreEqual(pagesModified, new bool[] { true, false, false });
        }
    }
}
