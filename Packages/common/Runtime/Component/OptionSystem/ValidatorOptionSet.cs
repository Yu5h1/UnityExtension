using UnityEngine;

namespace Yu5h1Lib
{
	public class ValidatorOptionSet : OptionSet<Validator> 
	{


        public void Validate()
        {
            if ($"{name} current validattor is null ".PopupWarningIf(current == null))
                return;
            current.Validate();
            
        }
    }
}