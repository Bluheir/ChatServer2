using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer2Client
{
	public class ConsoleReader
	{
		private const int STD_INPUT_HANDLE = -10;

		private readonly Action onCancel;

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr GetStdHandle(int nStdHandle);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

		public ConsoleReader(Action onCancel = null)
		{
			this.onCancel = onCancel;
		}

		public void Cancel()
		{
			var handle = GetStdHandle(STD_INPUT_HANDLE);
			CancelIoEx(handle, IntPtr.Zero);
		}

		public bool StartReading(out string output)
		{
			try
			{
				output = Console.ReadLine();
				return true;
			}
			catch(InvalidOperationException)
			{
				onCancel?.Invoke();
				output = null;
				return false;
			}
			catch(OperationCanceledException)
			{
				onCancel?.Invoke();
				output = null;
				return false;
			}
		}
		public static string CancellableReadLine(CancellationToken cancellationToken)
		{
			StringBuilder stringBuilder = new StringBuilder();
			Task.Run(() =>
			{
				try
				{
					ConsoleKeyInfo keyInfo;
					var startingLeft = Console.CursorLeft;
					var startingTop = Console.CursorTop;
					var currentIndex = 0;
					do
					{
						var previousLeft = Console.CursorLeft;
						var previousTop = Console.CursorTop;
						while (!Console.KeyAvailable)
						{
							cancellationToken.ThrowIfCancellationRequested();
							Thread.Sleep(50);
						}
						keyInfo = Console.ReadKey();
						switch (keyInfo.Key)
						{
							case ConsoleKey.A:
							case ConsoleKey.B:
							case ConsoleKey.C:
							case ConsoleKey.D:
							case ConsoleKey.E:
							case ConsoleKey.F:
							case ConsoleKey.G:
							case ConsoleKey.H:
							case ConsoleKey.I:
							case ConsoleKey.J:
							case ConsoleKey.K:
							case ConsoleKey.L:
							case ConsoleKey.M:
							case ConsoleKey.N:
							case ConsoleKey.O:
							case ConsoleKey.P:
							case ConsoleKey.Q:
							case ConsoleKey.R:
							case ConsoleKey.S:
							case ConsoleKey.T:
							case ConsoleKey.U:
							case ConsoleKey.V:
							case ConsoleKey.W:
							case ConsoleKey.X:
							case ConsoleKey.Y:
							case ConsoleKey.Z:
							case ConsoleKey.Spacebar:
							case ConsoleKey.Decimal:
							case ConsoleKey.Add:
							case ConsoleKey.Subtract:
							case ConsoleKey.Multiply:
							case ConsoleKey.Divide:
							case ConsoleKey.D0:
							case ConsoleKey.D1:
							case ConsoleKey.D2:
							case ConsoleKey.D3:
							case ConsoleKey.D4:
							case ConsoleKey.D5:
							case ConsoleKey.D6:
							case ConsoleKey.D7:
							case ConsoleKey.D8:
							case ConsoleKey.D9:
							case ConsoleKey.NumPad0:
							case ConsoleKey.NumPad1:
							case ConsoleKey.NumPad2:
							case ConsoleKey.NumPad3:
							case ConsoleKey.NumPad4:
							case ConsoleKey.NumPad5:
							case ConsoleKey.NumPad6:
							case ConsoleKey.NumPad7:
							case ConsoleKey.NumPad8:
							case ConsoleKey.NumPad9:
							case ConsoleKey.Oem1:
							case ConsoleKey.Oem102:
							case ConsoleKey.Oem2:
							case ConsoleKey.Oem3:
							case ConsoleKey.Oem4:
							case ConsoleKey.Oem5:
							case ConsoleKey.Oem6:
							case ConsoleKey.Oem7:
							case ConsoleKey.Oem8:
							case ConsoleKey.OemComma:
							case ConsoleKey.OemMinus:
							case ConsoleKey.OemPeriod:
							case ConsoleKey.OemPlus:
								stringBuilder.Insert(currentIndex, keyInfo.KeyChar);
								currentIndex++;
								if (currentIndex < stringBuilder.Length)
								{
									var left = Console.CursorLeft;
									var top = Console.CursorTop;
									Console.Write(stringBuilder.ToString().Substring(currentIndex));
									Console.SetCursorPosition(left, top);
								}
								break;
							case ConsoleKey.Backspace:
								if (currentIndex > 0)
								{
									currentIndex--;
									stringBuilder.Remove(currentIndex, 1);
									var left = Console.CursorLeft;
									var top = Console.CursorTop;
									if (left == previousLeft)
									{
										left = Console.BufferWidth - 1;
										top--;
										Console.SetCursorPosition(left, top);
									}
									Console.Write(stringBuilder.ToString().Substring(currentIndex) + " ");
									Console.SetCursorPosition(left, top);
								}
								else
								{
									Console.SetCursorPosition(startingLeft, startingTop);
								}
								break;
							case ConsoleKey.Delete:
								if (stringBuilder.Length > currentIndex)
								{
									stringBuilder.Remove(currentIndex, 1);
									Console.SetCursorPosition(previousLeft, previousTop);
									Console.Write(stringBuilder.ToString().Substring(currentIndex) + " ");
									Console.SetCursorPosition(previousLeft, previousTop);
								}
								else
									Console.SetCursorPosition(previousLeft, previousTop);
								break;
							case ConsoleKey.LeftArrow:
								if (currentIndex > 0)
								{
									currentIndex--;
									var left = Console.CursorLeft - 2;
									var top = Console.CursorTop;
									if (left < 0)
									{
										left = Console.BufferWidth + left;
										top--;
									}
									Console.SetCursorPosition(left, top);
									if (currentIndex < stringBuilder.Length - 1)
									{
										Console.Write(stringBuilder[currentIndex].ToString() + stringBuilder[currentIndex + 1]);
										Console.SetCursorPosition(left, top);
									}
								}
								else
								{
									Console.SetCursorPosition(startingLeft, startingTop);
									if (stringBuilder.Length > 0)
										Console.Write(stringBuilder[0]);
									Console.SetCursorPosition(startingLeft, startingTop);
								}
								break;
							case ConsoleKey.RightArrow:
								if (currentIndex < stringBuilder.Length)
								{
									Console.SetCursorPosition(previousLeft, previousTop);
									Console.Write(stringBuilder[currentIndex]);
									currentIndex++;
								}
								else
								{
									Console.SetCursorPosition(previousLeft, previousTop);
								}
								break;
							case ConsoleKey.Home:
								if (stringBuilder.Length > 0 && currentIndex != stringBuilder.Length)
								{
									Console.SetCursorPosition(previousLeft, previousTop);
									Console.Write(stringBuilder[currentIndex]);
								}
								Console.SetCursorPosition(startingLeft, startingTop);
								currentIndex = 0;
								break;
							case ConsoleKey.End:
								if (currentIndex < stringBuilder.Length)
								{
									Console.SetCursorPosition(previousLeft, previousTop);
									Console.Write(stringBuilder[currentIndex]);
									var left = previousLeft + stringBuilder.Length - currentIndex;
									var top = previousTop;
									while (left > Console.BufferWidth)
									{
										left -= Console.BufferWidth;
										top++;
									}
									currentIndex = stringBuilder.Length;
									Console.SetCursorPosition(left, top);
								}
								else
									Console.SetCursorPosition(previousLeft, previousTop);
								break;
							default:
								Console.SetCursorPosition(previousLeft, previousTop);
								break;
						}
					} while (keyInfo.Key != ConsoleKey.Enter);
					Console.WriteLine();
				}
				catch
				{
					//MARK: Change this based on your need. See description below.
					stringBuilder.Clear();
				}
			}).Wait();
			return stringBuilder.ToString();
		}
	}
}