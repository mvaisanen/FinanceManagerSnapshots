using System;

namespace Financemanager.Server.Database.Domain
{
    public class SSDScore
    {
        public int Id { get; set; }

        public int DivSafetyScore { get; set; }
        public DateTime ScoreDate { get; set; }
    }
}
