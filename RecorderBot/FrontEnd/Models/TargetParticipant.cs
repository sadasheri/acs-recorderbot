
namespace Sample.RecorderBot.Models
{
    public class TargetParticipant
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetParticipant"/> class.
        /// </summary>
        /// <param name="acsId">The acs id.</param>
        /// <param name="targetType">Whether the object is application.</param>
        /// <param name="displayName">The display name.</param>
        public TargetParticipant(string acsId, string acsId2, TargetType targetType)
        {
            this.ACSId = acsId;
            this.ACSId2 = acsId2;
            this.TargetType = targetType;
        }

        public TargetParticipant(string acsId, TargetType targetType)
        {
            this.ACSId = acsId;
            this.TargetType = targetType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetParticipant"/> class.
        /// </summary>
        private TargetParticipant()
        {
        }

        /// <summary>
        /// Gets or sets the acs id.
        /// </summary>
        public string ACSId { get; set; }
        public string ACSId2 { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the acs id is user.
        /// </summary>
        public TargetType TargetType { get; set; }
    }

    public enum TargetType
    {
        User //The Voip user
    }
}
