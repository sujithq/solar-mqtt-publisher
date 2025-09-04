# Solar MQTT Publisher - GitHub Copilot Instructions

**Always follow these instructions first and only fallback to additional search and context gathering if the information here is incomplete or found to be in error.**

Solar MQTT Publisher is a .NET 9 C# Home Assistant Add-on that publishes solar energy data (solar total, grid import, grid export) via MQTT Discovery to Home Assistant's Energy Dashboard. The application polls an API endpoint, processes energy data, and publishes it to an MQTT broker with automatic discovery for Home Assistant.

## Working Effectively

### Essential Prerequisites
**CRITICAL**: This project requires .NET 9 SDK. .NET 8 or earlier will not work.

Install .NET 9 SDK:
```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --runtime dotnet
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
```

Verify installation:
```bash
dotnet --version  # Should show 9.0.x
```

### Build and Test Commands

**Build the project** (takes ~2 seconds):
```bash
cd solar_mqtt_publisher
dotnet build src/SolarMqtt.csproj
```

**Build the solution** (takes ~1.5 seconds, recommended option):
```bash
cd solar_mqtt_publisher  
dotnet build solar-mqtt-publisher.sln
```

**NEVER CANCEL**: All build commands complete in under 5 seconds. Set timeout to 30+ seconds minimum.

### Running the Application

**Prepare configuration** (required before running):
```bash
cd solar_mqtt_publisher
mkdir -p data
cp sample-options.json data/options.json
```

**Run in development mode**:
```bash
cd solar_mqtt_publisher
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
src/bin/Debug/net9.0/SolarMqtt
```

**Expected behavior**: Application will start, log startup information, then fail trying to connect to MQTT broker at 127.0.0.1:1883. This is normal without a running MQTT broker.

### Docker Build Limitations

**DO NOT attempt Docker builds** - they fail due to SSL certificate issues with NuGet in the container environment:
```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json.
The SSL connection could not be established, see inner exception.
The remote certificate is invalid because of errors in the certificate chain: UntrustedRoot
```

This is a known limitation of the containerized build environment.

## Validation and Testing

**CRITICAL**: There are no automated tests in this repository. Manual validation is required.

### Manual Validation Scenarios

**Always run this complete validation after making changes**:

1. **Build Validation**:
   ```bash
   cd solar_mqtt_publisher
   dotnet build solar-mqtt-publisher.sln
   ```
   Expected: Build succeeds in ~1 second with no errors.

2. **Configuration Validation**:
   ```bash
   mkdir -p data
   cp sample-options.json data/options.json
   ```
   Expected: Configuration file copied successfully.

3. **Runtime Validation**:
   ```bash
   export PATH="$HOME/.dotnet:$PATH"
   export DOTNET_ROOT="$HOME/.dotnet"
   timeout 5s src/bin/Debug/net9.0/SolarMqtt
   ```
   Expected output:
   - Startup log showing MQTT broker=127.0.0.1:1883
   - "Connecting to MQTT broker..." message
   - Connection failure due to no MQTT broker (this is expected)

4. **Configuration Loading Test**:
   Verify the application reads the configuration correctly by checking the startup log shows the expected values from options.json.

### No Linting or Additional Tools

**Important**: This repository has no linting, formatting, or testing tools configured. There are no CI/CD workflows to validate changes. All validation must be done manually using the scenarios above.

## Repository Navigation

### Key Directories and Files

**Project Structure**:
```
solar_mqtt_publisher/
├── src/
│   ├── SolarMqtt.csproj          # Main project file
│   ├── Program.cs                # Main application entry point
│   ├── Options.cs                # Configuration model
│   ├── ApiClient.cs              # API polling logic
│   └── MqttPublisher.cs          # MQTT publishing logic
├── solar-mqtt-publisher.sln      # Solution file (build this)
├── sample-options.json           # Configuration template
├── data/options.json             # Runtime config (git ignored)
├── Dockerfile                    # Docker build (doesn't work)
├── config.yaml                   # Home Assistant add-on config
└── run.sh                        # Add-on entry script
```

**Always work from the `solar_mqtt_publisher/` directory** - this is where the solution and project files are located.

### Important File Relationships

- `sample-options.json` → `data/options.json` (copy for runtime)
- Configuration is loaded from `data/options.json` by default
- The application looks for the config file relative to the current working directory

### Common Code Areas

**When modifying configuration**:
- Check `Options.cs` for configuration model
- Update `sample-options.json` template if needed
- Remember to copy to `data/options.json` for testing

**When modifying MQTT behavior**:
- Main logic in `MqttPublisher.cs`
- Publishing calls in `Program.cs` main loop

**When modifying API polling**:
- API client logic in `ApiClient.cs`
- Data processing in `Program.cs` main loop

## Timing Expectations

- **.NET 9 SDK install**: ~60 seconds for download and installation
- **Build (project)**: ~2 seconds - **NEVER CANCEL**
- **Build (solution)**: ~1.5 seconds - **NEVER CANCEL**  
- **Application startup**: Immediate (~1 second to show logs)
- **Docker build**: Don't attempt - known to fail

## Common Tasks

### Making Configuration Changes

1. Edit `sample-options.json` if changing template
2. Copy to `data/options.json` for testing: `cp sample-options.json data/options.json`
3. Test with: `src/bin/Debug/net9.0/SolarMqtt`
4. Verify configuration loads correctly from startup logs

### Adding New Dependencies

1. Edit `src/SolarMqtt.csproj`
2. Run `dotnet restore src/SolarMqtt.csproj`
3. Build and test as usual

### Debugging Connection Issues

Check the startup logs to verify:
- MQTT broker configuration is correct
- API URL configuration is loaded properly
- Poll interval is set as expected

Expected connection failure without MQTT broker is normal and indicates the application is working correctly.

## Security Notes

- **Never commit `data/options.json`** - it's git ignored for security
- Only edit `sample-options.json` as the public template
- Real MQTT credentials and API keys should only be in the runtime config
- The `data/` directory is excluded from git to prevent credential leaks