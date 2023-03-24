using System;
using System.IO;
using System.Collections;
using System.Text;

namespace TCParser
{
    public interface IVariableContext
    {        
        float ResolveVariable(string name);
    }
}
