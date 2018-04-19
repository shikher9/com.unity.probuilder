using System;
using System.Linq;
using System.Collections.Generic;

namespace ProBuilder.BuildSystem
{
	/**
	 *	Describes a set of commands to be executed during a build.
	 */
	public class BuildCommand : IExpandMacros
	{
		/**
		 *	Copy command.
		 *	Copies files or folders from source to destination.
		 *	JSON: cp
		 *	ARGS: (string source, string destination)
		 */
		public const string COPY = "cp";

		/**
		 *	Delete command.
		 *	Remove files or folders.
		 *	JSON: rm
		 *	ARGS: (string path)
		 */
		public const string DELETE = "rm";

		/**
		 *	Create new directory.
		 */
		public const string MKDIR = "mkdir";

		// Find the first occurence of a regex pattern in a file.
		public const string FIND = "find";

		// Replace all matched strings with contents.
		public const string REPLACE = "replace";

		// The command to execute.
		public string Command;

		// Any arguments needed by the executing command. Ex, `cp` takes source and destination.
		public List<string> Arguments;

		public void Replace(string key, string replace)
		{
			for(int i = 0; i < (Arguments != null ? Arguments.Count : 0); i++)
			{
				Arguments[i] = Arguments[i].Replace(key, replace);
			}
		}

		public override string ToString()
		{
			return string.Format("{0} {1}", Command, string.Join(",", Arguments.ToArray()));
		}
	}
}