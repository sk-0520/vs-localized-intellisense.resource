using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsLocalizedIntellisense.Models.Service.CommandPrompt
{
    public abstract class ActionBase
    {
        #region property

        public virtual string CommandName { get; set; }

        #endregion

        #region function

        public abstract string GetStatement();

        public virtual string ToStatement(int indentLevel, string indentValue)
        {
            return GetStatement();
        }

        #endregion
    }
}
