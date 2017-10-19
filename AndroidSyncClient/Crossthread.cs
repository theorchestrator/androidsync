using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;

public delegate void Action();
public delegate void Action<T1, T2>(T1 Arg1, T2 Arg2);
public delegate void Action<T1, T2, T3>(T1 Arg1, T2 Arg2, T3 Arg3);
public delegate void Action<T1, T2, T3, T4>(T1 Arg1, T2 Arg2, T3 Arg3, T4 Arg4);

public class CrossThread
{

    public static void RunAsync<T1, T2, T3, T4>(Action<T1, T2, T3, T4> Action, T1 Arg1, T2 Arg2, T3 Arg3, T4 Arg4)
    {
        Action.BeginInvoke(Arg1, Arg2, Arg3, Arg4, Action.EndInvoke, null);
    }

    public static void RunAsync<T1, T2, T3>(Action<T1, T2, T3> Action, T1 Arg1, T2 Arg2, T3 Arg3)
    {
        Action.BeginInvoke(Arg1, Arg2, Arg3, Action.EndInvoke, null);
    }

    public static void RunAsync<T1, T2>(Action<T1, T2> Action, T1 Arg1, T2 Arg2)
    {
        Action.BeginInvoke(Arg1, Arg2, Action.EndInvoke, null);
    }

    public static void RunAsync<T1>(Action<T1> Action, T1 Arg1)
    {
        Action.BeginInvoke(Arg1, Action.EndInvoke, null);
    }

    public static void RunAsync(Action Action)
    {
        Action.BeginInvoke(Action.EndInvoke, null);
    }

    private static bool GuiCrossInvoke(Delegate Action, params object[] Args)
    {
        if (Application.OpenForms.Count == 0)
        {
            return true;
        }
        if (Application.OpenForms[0].InvokeRequired)
        {
            Application.OpenForms[0].BeginInvoke(Action, Args);
            return true;
        }
        return false;
    }

    public static void RunGui<T1, T2, T3>(Action<T1, T2, T3> Action, T1 Arg1, T2 Arg2, T3 Arg3)
    {
        if (!GuiCrossInvoke(Action, Arg1, Arg2, Arg3))
            Action(Arg1, Arg2, Arg3);
    }

    public static void RunGui<T1, T2>(Action<T1, T2> Action, T1 Arg1, T2 Arg2)
    {
        if (!GuiCrossInvoke(Action, Arg1, Arg2))
            Action(Arg1, Arg2);
    }

    public static void RunGui<T1>(Action<T1> Action, T1 Arg1)
    {
        if (!GuiCrossInvoke(Action, Arg1))
            Action(Arg1);
    }

    public static void RunGui(Action Action)
    {
        if (!GuiCrossInvoke(Action))
            Action();
    }
}
