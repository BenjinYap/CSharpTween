/*
The MIT License (MIT)

Copyright (c) 2014 Benjin Yap

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace CSharpTween {
	
	public static class Tweener {
		public static Dictionary <Type, Func <object, object, float, object>> steppers = new Dictionary<Type,Func<object,object,float,object>> ();

		private static Dictionary <object, List <Tween>> tweens = new Dictionary<object, List <Tween>> ();
		private static Dictionary <object, List <Tween>> toAdd = new Dictionary<object,List<Tween>> ();
		private static Dictionary <object, List <Tween>> toRemove = new Dictionary<object,List<Tween>> ();

		static Tweener () {
			RegisterStepper <float> ((start, end, step) => {
				return step * ((float) end - (float) start) + (float) start;
			});
			RegisterStepper <int> ((start, end, step) => {
				return (int) (step * ((int) end - (int) start) + (int) start);
			});
		}

		public static void Update () {
			toRemove.Keys.ToList ().ForEach (owner => {
				toRemove [owner].ForEach (tween => {
					tweens [owner].Remove (tween);
				});
			});
			toRemove.Clear ();
			
			tweens.Values.ToList ().ForEach (list => {
				list.RemoveAll (tween => tween.complete);
				int count = list.Count;

				for (int i = 0; i < count; i++) {
					list [i].Update ();
				}
			});

			toAdd.Keys.ToList ().ForEach (owner => {
				toAdd [owner].ForEach (tween => {
					if (tweens.ContainsKey (owner) == false) {
						tweens [owner] = new List<Tween> ();
					}

					tweens [owner].Add (tween);
				});
			});
			toAdd.Clear ();
		}

		public static void RegisterStepper <T> (Func <object, object, float, object> func) {
			steppers [typeof (T)] = func;
		}

		public static Tween Add (object owner, Action <object> setter, object start, object end, int totalSteps, Action completeHandler) {
			if (toAdd.ContainsKey (owner) == false) {
				toAdd [owner] = new List<Tween> ();
			}
			
			Tween tween = new Tween (steppers [start.GetType ()], setter, start, end, totalSteps, completeHandler);
			toAdd [owner].Add (tween);
			return tween;
		}
		
		/// <summary>
		/// Removes all Tweens owned by an object.
		/// </summary>
		/// <param name="owner"></param>
		public static void RemoveAll (object owner) {
			if (tweens.ContainsKey (owner) == false) {
				return;
			}

			if (tweens [owner].Count > 0) {
				tweens [owner].ForEach (tween => Remove (owner, tween));
			}
		}

		/// <summary>
		/// Removes a Tween.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="tween"></param>
		public static void Remove (object owner, Tween tween) {
			if (toRemove.ContainsKey (owner) == false) {
				toRemove [owner] = new List<Tween> ();
			}

			if (toRemove [owner].Contains (tween) == false) {
				toRemove [owner].Add (tween);
			}
		}
	}

	public class Tween {
		private Action completeHandler;
		private object value;

		private Func <object, object, float, object> stepper;
		private Action <object> setter;
		private object start;
		private object end;
		private int step = 0;
		private int totalSteps;
		public bool complete = false;

		public Tween (Func <object, object, float, object> stepper, Action <object> setter, object startValue, object endValue, int totalSteps, Action completeHandler) {
			this.stepper = stepper;
			this.setter = setter;
			this.start = startValue;
			this.end = endValue;
			this.totalSteps = totalSteps;
			this.completeHandler = completeHandler;
		}

		public void Remove (object owner) {
			Tweener.Remove (owner, this);
		}

		public void Update () {
			value = stepper (start, end, (float) step / totalSteps);
			setter (value);
			step++;
			
			if (step == totalSteps + 1) {
				if (completeHandler != null) {
					completeHandler ();
					complete = true;
				}
			}
		}
	}
}
