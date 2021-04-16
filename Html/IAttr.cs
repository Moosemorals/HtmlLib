using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public interface IAttr : INode {

        string? NamespaceURI { get; }
        string? Prefix { get; }
        string LocalName { get; }
        string Name { get; }

        string Value { get; set; }

        IElement? OwnerElement { get; }

        bool Specified => true;

    }
}
