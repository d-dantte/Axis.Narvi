﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Narvi.Notify
{
    public interface INotificationSubscription
    {
        void Unsubscribe();
    }
}
