﻿using System;

namespace ModTek.Common.Utils.LogStreamImpl;

internal interface ILogStream : IDisposable
{
    public void Append(byte[] bytes, int srcOffset, int count);
}