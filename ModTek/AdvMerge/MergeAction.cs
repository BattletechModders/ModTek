namespace ModTek.AdvMerge
{
    internal enum MergeAction
    {
        ArrayAdd, // adds a given value to the end of the target array
        ArrayAddAfter, // adds a given value after the target element in the array
        ArrayAddBefore, // adds a given value before the target element in the array
        ArrayConcat, // adds a given array to the end of the target array
        ObjectMerge, // merges a given object with the target objects
        Remove, // removes the target element(s)
        Replace // replaces the target with a given value
    }
}
