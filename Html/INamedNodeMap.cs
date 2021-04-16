using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public interface INamedNodeMap {

        long Length { get; }
        IAttr? Item(long index);
        IAttr? GetNamedItem(string qualifiedName);
        IAttr? GetNamedItemNS(string? @namespace, string localName);

        IAttr? SetNamedItem(IAttr attr);
        IAttr? SetNamedItemNS(IAttr attr);
        IAttr? RemoveNamedItem(string qualifiedName);
        IAttr? RemoveNamedItemNS(string @namespace, string localName);



    }
}
