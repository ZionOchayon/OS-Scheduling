using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
    class IdleCode : Code
    {
        public IdleCode() : base()
        {
            //your code here
            String newLine3 = "goto 0";
            String newLine1 = "write \"Test \"";
            String newLine2 = "yield";
            m_lLines.Add(newLine1);
            m_lLines.Add(newLine2);
            m_lLines.Add(newLine3);      
        }
    }
}
