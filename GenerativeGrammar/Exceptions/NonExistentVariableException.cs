namespace GenerativeGrammar.Exceptions;

public class NonExistentVariableException : Exception
{
    private readonly string _variable;
    
    public NonExistentVariableException(string variable)
    {
        this._variable = variable;
    }
    
    public override string ToString()
    {
        return "Variable " + this._variable + " does not exist";
    }
}