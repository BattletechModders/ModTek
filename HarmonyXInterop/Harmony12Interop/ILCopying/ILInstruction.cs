using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Harmony.ILCopying
{
	public enum ExceptionBlockType
	{
		BeginExceptionBlock,
		BeginCatchBlock,
		BeginExceptFilterBlock,
		BeginFaultBlock,
		BeginFinallyBlock,
		EndExceptionBlock
	}

	public class ExceptionBlock
	{
		public ExceptionBlockType blockType;
		public Type catchType;

		public ExceptionBlock(ExceptionBlockType blockType, Type catchType)
		{
			this.blockType = blockType;
			this.catchType = catchType;
		}
	}
}
