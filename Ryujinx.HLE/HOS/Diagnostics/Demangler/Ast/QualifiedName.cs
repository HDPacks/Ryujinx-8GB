using System.IO;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast
{
    public class QualifiedName : BaseNode
    {
        private BaseNode _qualifier;
        private BaseNode _name;

        public QualifiedName(BaseNode qualifier, BaseNode name) : base(NodeType.QualifiedName)
        {
            _qualifier = qualifier;
            _name      = name;
        }

        public override void PrintLeft(TextWriter writer)
        {
            _qualifier.Print(writer);
            writer.Write("::");
            _name.Print(writer);
        }
    }
}