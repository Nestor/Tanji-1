namespace Tangine.Habbo
{
    /// <summary>
    /// Specifies speech modes that users can communicate with each other in the client.
    /// </summary>
    public enum HSpeech
    {
        /// <summary>
        /// Represents a speech mode that makes a message publicly visible within a determined range in the room.
        /// </summary>
        Say = 0,
        /// <summary>
        /// Represents a speech mode that makes a message publicly visible to everyone in the room regardless of range.
        /// </summary>
        Shout = 1,
        /// <summary>
        /// Represents a speech mode that makes the message visible only to a specific user regardless of range.
        /// </summary>
        Whisper = 2
    }
}