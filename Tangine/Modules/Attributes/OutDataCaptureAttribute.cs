namespace Tangine.Modules
{
    public sealed class OutDataCaptureAttribute : DataCaptureAttribute
    {
        public OutDataCaptureAttribute(ushort id)
            : base(id, true)
        { }
        public OutDataCaptureAttribute(string hash)
            : base(hash, true)
        { }
    }
}