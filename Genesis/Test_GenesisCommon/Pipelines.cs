using System;
using LibZConfig.Common.Config.Attributes;
using LibGenesisCommon.Process;

namespace LibGenesisCommon.Tests
{
    public class CheckNameProcessor : BasicProcessor<User>
    {
        protected override ProcessResponse<User> ExecuteProcess(User data)
        {
            ProcessResponse<User> response = new ProcessResponse<User>();
            if (String.IsNullOrWhiteSpace(data.LastName))
            {
                response.Data = data;
                response.SetError(EProcessResponse.FatalError, new ProcessException(String.Format("Invalid User: Missing {0}", nameof(data.LastName))));
            }
            else if (String.IsNullOrWhiteSpace(data.FirstName))
            {
                response.Data = data;
                response.SetError(EProcessResponse.ContinueWithError, new ProcessException(String.Format("Invalid User: Missing {0}", nameof(data.FirstName))));
            }
            return response;
        }
    }

    public class DefaultEmailProcessor : BasicProcessor<User>
    {
        [ConfigValue(Name = "domain", Required = true)]
        public string Domain { get; set; }

        protected override ProcessResponse<User> ExecuteProcess(User data)
        {
            ProcessResponse<User> response = new ProcessResponse<User>();
            if (String.IsNullOrWhiteSpace(data.EmailId))
            {
                data.EmailId = String.Format("{0}.{1}@{2}", data.FirstName, data.LastName, Domain);
                response.Data = data;
                response.State = EProcessResponse.OK;
            }
            return response;
        }
    }

    public class AgeValidator : BasicProcessor<User>
    {
        [ConfigAttribute(Name = "adultAge", Required = true)]
        public int AdultAge { get; set; }

        protected override ProcessResponse<User> ExecuteProcess(User data)
        {
            ProcessResponse<User> response = new ProcessResponse<User>();
            if (data.DateOfBirth == null)
            {
                response.Data = data;
                response.SetError(EProcessResponse.FatalError, new ProcessException(String.Format("Invalid User: Missing {0}", nameof(data.DateOfBirth))));
            }
            if (String.IsNullOrWhiteSpace(data.EmailId))
            {
                TimeSpan ts = DateTime.Now - data.DateOfBirth;
                if (ts.TotalDays/365 > AdultAge)
                {
                    data.IsAdult = true;
                }
                else
                {
                    data.IsAdult = false;
                }
                response.Data = data;
                response.State = EProcessResponse.OK;
            }
            return response;
        }
    }

    public class UserDataPipeline : BasicPipeline<User>
    {

    }
}