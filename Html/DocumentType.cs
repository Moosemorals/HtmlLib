using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    internal class DocumentType : Node, IDocumentType {
        public DocumentType(string name, string publicId = "", string systemId = "") : base(name) {
            Name=name;
            PublicId=publicId;
            SystemId=systemId;
        }


        public string Name { get; init; }

        public string PublicId { get; init; }

        public string SystemId { get; init; }

        public override NodeType NodeType => NodeType.DocumentType;

        public override string? TextContent => null;

        public override INode CloneNode(bool deep = false) {
            return new DocumentType(Name, PublicId, SystemId);
        }
    }
}
