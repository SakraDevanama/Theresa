using System;
using System.Linq;
using System.Reflection;
class Program {
    static void Main() {
        var asm = Assembly.LoadFrom(@"D:\steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll");
        var type = asm.GetType("MegaCrit.Sts2.Core.Models.AbstractModel");
        if (type != null) {
            var method = type.GetMethod("ModifyHpLostBeforeOsty", BindingFlags.Public | BindingFlags.Instance);
            if (method != null) {
                Console.WriteLine("ModifyHpLostBeforeOsty found:");
                Console.WriteLine("Return: " + method.ReturnType.FullName);
                foreach (var p in method.GetParameters()) {
                    Console.WriteLine("Param: " + p.ParameterType.Name + " " + p.Name);
                }
            }
        }
    }
}
