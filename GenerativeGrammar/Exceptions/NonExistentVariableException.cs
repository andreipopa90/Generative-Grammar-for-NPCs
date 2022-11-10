namespace GenerativeGrammar.Exceptions
{
    public class NonExistentVariableException : Exception
    {
        private readonly string _variable;
    
        public NonExistentVariableException(string variable)
        {
            _variable = variable;
        }
    
        public override string ToString()
        {
            return "Variable " + _variable + " does not exist";
        }
    }
}