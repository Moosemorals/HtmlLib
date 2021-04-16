using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public class DOMException : Exception {
        public DOMException(string? message) : base(message) {
        }
    }
}
