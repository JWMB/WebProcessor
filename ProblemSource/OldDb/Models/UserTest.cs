using System;
using System.Collections.Generic;

namespace OldDb.Models
{
    public partial class UserTest
    {
        public int Id { get; set; }
        public int PhaseId { get; set; }
        public long Time { get; set; }
        public bool? Ended { get; set; }
        public bool CompletedPlanet { get; set; }
        public int Incorrects { get; set; }
        public int Corrects { get; set; }
        public int Questions { get; set; }
        public int Score { get; set; }
        public int TargetScore { get; set; }
        public int PlanetTargetScore { get; set; }
        public bool WonRace { get; set; }

        public virtual Phase Phase { get; set; } = null!;
    }
}
