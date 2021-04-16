using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public interface IHTMLCollection {
        public long Length { get; }
        public IElement? NamedItem(string name);

        public IElement? this[long index] { get; }
        public IElement? this[string name] { get; }
    }
}
