namespace System.CodeDom.Compiler
{
    public class CompilerError
    {
        public CompilerError()
        {
        }

        public CompilerError(string? fileName, int line, int column, string? errorNumber, string? errorText)
        {
            ErrorText = errorText;
            IsWarning = false;
        }

        public string? ErrorText { get; set; }
        public bool IsWarning { get; set; }
    }

    public class CompilerErrorCollection
    {
        public void Add(CompilerError error)
        {
        }
    }
}