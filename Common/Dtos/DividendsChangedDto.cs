using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DividendsChangedDto
    {
        public List<ReceivedDividendDTO> DividendsAddedOrModified = new List<ReceivedDividendDTO>();
        public List<ReceivedDividendDTO> DividendsRemoved = new List<ReceivedDividendDTO>();
    }
}
