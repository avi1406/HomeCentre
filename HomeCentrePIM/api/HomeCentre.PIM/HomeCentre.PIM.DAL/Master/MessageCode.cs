using System.Runtime.Serialization;

namespace HomeCentre.PIM.DAL.Master
{
    /// <summary>
    /// Message Codes/Labels used for localization
    /// </summary>
    /// <seealso cref="Entity" />
    public class MessageCode : Entity
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        [DataMember]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        [DataMember]
        public string Message { get; set; }
    }
}