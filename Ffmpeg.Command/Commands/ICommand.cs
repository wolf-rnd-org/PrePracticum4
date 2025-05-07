using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ffmpeg.Command.Commands
{
    public interface ICommand<TRequest>
    {
        Task<Result> ExecuteAsync(TRequest request);
    }
}
