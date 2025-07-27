using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        private Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, string.Empty);
        public static Result Failure(string error) => new Result(false, error);
    }

}
