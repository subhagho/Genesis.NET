# Genesis.NET

[![Build Status](https://travis-ci.org/subhagho/Genesis.NET.svg?branch=master)](https://travis-ci.org/subhagho/Genesis.NET)


Auto-wired data pipelines
- Define Data pipelines
- Define Processors to operate on the data/data collections
- Condition based processor invocation

## Processors
Operating units that are grouped to form a pipeline. Processors can be used to 
validate, filter, decorate data elements.

### Base Processor
Processors to operate on data elements.

__Sample:__
```csharp
    public class CheckNameProcessor : BasicProcessor<User>
    {
        protected override ProcessResponse<User> ExecuteProcess(User data, Context context, ProcessResponse<User> response)
        {
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
```

### Collection Processor
Processors defined to operate on Collection of data elements.

## Configuration
Pipeline/Processor definitions are loaded using XML configurations.

ZConfig Configuration library is used to define the Pipeline configurations.

https://github.com/subhagho/Zconfig.NET

__Sample Configuration:__

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <header ID="UNIQUE-99918239013" group="TEST-APP-GROUP" application="TEST-APPLICATION" name="test-pipelines" version="0.2">
    <description>This is a test Pipeline configuration file.</description>
    <createdBy user="subho" timestamp="1552835141000" />
    <updatedBy user="subho" timestamp="1552835151000" />
  </header>
  <pipelines>
    <pipeline name="UserDataPipeline" type="LibGenesisCommon.Tests.UserDataPipeline" assembly="Test_GenesisCommon.dll">
      <processors>
        <processor name="CheckNameProcessor" type="LibGenesisCommon.Tests.CheckNameProcessor" assembly="Test_GenesisCommon.dll"/>
        <processor name="DefaultEmailProcessor" type="LibGenesisCommon.Tests.DefaultEmailProcessor" assembly="Test_GenesisCommon.dll"/>
        <processor name="AgeValidator" type="LibGenesisCommon.Tests.AgeValidator" assembly="Test_GenesisCommon.dll" adultAge="18">
          <condition clause="user.FirstName != null" typeName="user"/>
        </processor>
      </processors>
    </pipeline>
    <pipeline name="UserDataPipelineRef" type="LibGenesisCommon.Tests.UserDataPipeline" assembly="Test_GenesisCommon.dll">
      <processors>
        <processor name="process" reference="UserDataPipeline"/>
      </processors>
    </pipeline>
  </pipelines>
</configuration>
```

