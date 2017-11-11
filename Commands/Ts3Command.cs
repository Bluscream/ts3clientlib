// TS3Client - A free TeamSpeak3 client implementation
// Copyright (C) 2017  TS3Client contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

namespace TS3Client.Commands
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using System.Text.RegularExpressions;

	/// <summary>Builds TeamSpeak (query) commands from parameters.</summary>
	public sealed class Ts3Command
	{
		private static readonly Regex CommandMatch = new Regex(@"[a-z0-9_]+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ECMAScript);
		public static List<ICommandPart> NoParameter => new List<ICommandPart>();

		internal bool ExpectResponse { get; set; }
		public string Command { get; }
		private readonly List<ICommandPart> parameter;
		
		/// <summary>Creates a new command.</summary>
		/// <param name="command">The command name.</param>
		[DebuggerStepThrough]
		public Ts3Command(string command) : this(command, NoParameter) { }
		/// <summary>Creates a new command.</summary>
		/// <param name="command">The command name.</param>
		/// <param name="parameter">The parameters to be added to this command.
		/// See <see cref="CommandParameter"/>, <see cref="CommandOption"/> or <see cref="CommandMultiParameter"/> for more information.</param>
		[DebuggerStepThrough]
		public Ts3Command(string command, List<ICommandPart> parameter)
		{
			ExpectResponse = true;
			this.Command = command;
			this.parameter = parameter;
		}

		public Ts3Command AppendParameter(ICommandPart addParameter)
		{
			parameter.Add(addParameter);
			return this;
		}

		internal Ts3Command ExpectsResponse(bool expects)
		{
			ExpectResponse = expects;
			return this;
		}

		/// <summary>Builds this command to the query-like command.</summary>
		/// <returns>The formatted query-like command.</returns>
		public override string ToString() => BuildToString(Command, parameter);

		/// <summary>Builds the command from its parameters and returns the query-like command.</summary>
		/// <param name="command">The command name.</param>
		/// <param name="parameter">The parameter to be added to this command.</param>
		/// <returns>The formatted query-like command.</returns>
		/// <exception cref="ArgumentException">When a command is null or not valid.</exception>
		/// <exception cref="ArgumentOutOfRangeException">When multiple <see cref="CommandMultiParameter"/> are added but have different array lengths.</exception>
		public static string BuildToString(string command, IEnumerable<ICommandPart> parameter)
		{
			if (string.IsNullOrWhiteSpace(command))
				throw new ArgumentNullException(nameof(command));
			if (!CommandMatch.IsMatch(command))
				throw new ArgumentException("Invalid command characters", nameof(command));

			var strb = new StringBuilder(Ts3String.Escape(command));
			List<CommandMultiParameter> multiParamList = null;
			List<CommandOption> optionList = null;

			foreach (var param in parameter)
			{
				switch (param.Type)
				{
				case CommandPartType.SingleParameter:
					var singleParam = (CommandParameter)param;
					strb.Append(' ').Append(singleParam.Key).Append('=').Append(singleParam.Value);
					break;
				case CommandPartType.MultiParameter:
					if (multiParamList == null)
						multiParamList = new List<CommandMultiParameter>();
					multiParamList.Add((CommandMultiParameter)param);
					break;
				case CommandPartType.Option:
					if (optionList == null)
						optionList = new List<CommandOption>();
					optionList.Add((CommandOption)param);
					break;
				default:
					throw Util.UnhandledDefault(param.Type);
				}
			}

			if (multiParamList != null)
			{
				// Safety check
				int matrixLength = multiParamList[0].Values.Length;
				foreach (var param in multiParamList)
					if (param.Values.Length != matrixLength)
						throw new ArgumentOutOfRangeException(nameof(parameter), "All multiparam key-value pairs must have the same length");

				for (int i = 0; i < matrixLength; i++)
				{
					foreach (var param in multiParamList)
					{
						strb.Append(' ').Append(param.Key).Append('=').Append(param.Values[i]);
					}
					strb.Append('|');
				}

				strb.Length--;
			}

			if (optionList != null)
			{
				foreach (var option in optionList)
					strb.Append(option.Value);
			}

			return strb.ToString();
		}
	}
}
