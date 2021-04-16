using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public interface IDocumentType : INode {
        public string Name { get; }
        public string PublicId { get; }
        public string SystemId { get; }
    }
}
