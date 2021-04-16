using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public interface INodeList : IEnumerable<INode> {

        public INode? Item(long index);
        public long Length { get; }

    }
}
