﻿/*
CoordinateSharp is a .NET standard library that is intended to ease geographic coordinate 
format conversions and location based celestial calculations.
https://github.com/Tronald/CoordinateSharp

Many celestial formulas in this library are based on Jean Meeus's 
Astronomical Algorithms (2nd Edition). Comments that reference only a chapter
are referring to this work.

License

CoordinateSharp is split licensed and may be licensed under the GNU Affero General Public License version 3 or a commercial use license as stated.

Copyright (C) 2019, Signature Group, LLC
  
This program is free software; you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 
as published by the Free Software Foundation with the addition of the following permission added to Section 15 as permitted in Section 7(a): 
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY Signature Group, LLC. Signature Group, LLC DISCLAIMS THE WARRANTY OF 
NON INFRINGEMENT OF THIRD PARTY RIGHTS.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details. You should have received a copy of the GNU 
Affero General Public License along with this program; if not, see http://www.gnu.org/licenses or write to the 
Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA, 02110-1301 USA, or download the license from the following URL:

https://www.gnu.org/licenses/agpl-3.0.html

The interactive user interfaces in modified source and object code versions of this program must display Appropriate Legal Notices, 
as required under Section 5 of the GNU Affero General Public License.

You can be released from the requirements of the license by purchasing a commercial license. Buying such a license is mandatory 
as soon as you develop commercial activities involving the CoordinateSharp software without disclosing the source code of your own applications. 
These activities include: offering paid services to customers as an ASP, on the fly location based calculations in a web application, 
or shipping CoordinateSharp with a closed source product.

Organizations or use cases that fall under the following conditions may receive a free commercial use license upon request.
-Department of Defense
-Department of Homeland Security
-Open source contributors to this library
-Scholarly or scientific uses on a case by case basis.
-Emergency response / management uses on a case by case basis.

For more information, please contact Signature Group, LLC at this address: sales@signatgroup.com
*/
using System;
using System.Diagnostics;
using System.Reflection;

namespace CoordinateSharp.Debuggers
{
    /// <summary>
    /// Debugging Write Tools
    /// </summary>
    public static class Output
    {
        /// <summary>
        /// Output all public property values of a class.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="opt">Output Option</param>
        public static void Output_Class_Values(object obj, OutputOption opt)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (opt == OutputOption.Console)
                {
                    Console.WriteLine(property.Name + ": " + property.GetValue(obj, null));
                }
                else if (opt == OutputOption.Debugger)
                {
                    Debug.WriteLine(property.Name + ": " + property.GetValue(obj, null));
                }
            }
        }

        /// <summary>
        /// Output all public property values of a class.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="opt">Output Option</param>
        /// <param name="leadingString">Output Leading String</param>
        public static void Output_Class_Values(object obj, OutputOption opt, string leadingString)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (opt == OutputOption.Console)
                {
                    Console.WriteLine(leadingString + property.Name + ": " + property.GetValue(obj, null));
                }
                else if (opt == OutputOption.Debugger)
                {
                    Debug.WriteLine(leadingString + property.Name + ": " + property.GetValue(obj, null));
                }
            }
        }
    }
    /// <summary>
    /// Debugger output option.
    /// </summary>
    public enum OutputOption
    {
        /// <summary>
        /// Writes output to debugger output window.
        /// </summary>
        Debugger,

        /// <summary>
        /// Writes output to console.
        /// </summary>
        Console
    }
}
