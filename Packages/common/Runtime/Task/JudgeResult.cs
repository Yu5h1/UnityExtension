namespace Yu5h1Lib
{
    /// <summary>
    /// What a <see cref="ResultJudge"/> decided about a job's End content: success or failure, and the content
    /// handed to its True or False listeners.
    /// </summary>
    public readonly struct JudgeResult
    {
        public readonly bool Succeeded;
        public readonly string Content;

        public JudgeResult(bool succeeded, string content)
        {
            Succeeded = succeeded;
            Content = content;
        }

        public static implicit operator bool(JudgeResult result) => result.Succeeded;

        public override string ToString() => $"{(Succeeded ? "Succeeded" : "Failed")}: {Content}";
    }
}
