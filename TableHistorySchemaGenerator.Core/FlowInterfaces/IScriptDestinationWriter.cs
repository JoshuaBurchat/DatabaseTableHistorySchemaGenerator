﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableHistorySchemaGenerator.Core.Models;

namespace TableHistorySchemaGenerator.Core
{
    public interface IScriptDestinationWriter
    {
        void Commit(ChangeScript[] scripts);
    }
}
