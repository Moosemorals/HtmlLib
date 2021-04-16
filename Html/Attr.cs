using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    internal class Attr : Node, IAttr {
        public Attr(
            string name,
            string value = "",
            string? @namespace = null,
            string? prefix = null
            ) : base(prefix == null ? name : string.Join(':', prefix, name)) {

            LocalName = name;
            Value = value;
            NamespaceURI = @namespace;
            Prefix = prefix;
        }

        public string? NamespaceURI { get; init; }

        public string? Prefix { get; init; }

        public string LocalName { get; init; }

        public string Value { get; set; }

        public override string? NodeValue {
            get => Value;
            internal set => Value =  value ?? "";
        }

        public IElement? OwnerElement => ParentNode as IElement;

        public override NodeType NodeType => NodeType.Attribute;

        public override string? TextContent => Value;

        public string Name => NodeName;

        public override INode CloneNode(bool deep = false) => throw new NotImplementedException();
    }
}
