set shell := ["bash", "-euo", "pipefail", "-c"]
set windows-shell := ["pwsh", "-NoLogo", "-NoProfile", "-Command"]

configuration := env_var_or_default("BUILD_CONFIGURATION", "Debug")
package_output_path := env_var_or_default("PACKAGE_OUTPUT_PATH", ".artifacts/packages")
test_results_path := env_var_or_default("TEST_RESULTS_PATH", ".artifacts/test-results")

default:
	@just --list

restore:
	dotnet tool restore
	dotnet restore

restore-locked:
	dotnet restore --locked-mode

outdated:
	dotnet outdated

outdated-dotnet:
	dotnet package list --outdated

upgrade:
	dotnet outdated --upgrade:auto

build:
	dotnet build --configuration {{ configuration }} --no-restore

clean:
	dotnet clean --configuration {{ configuration }}

format:
	dotnet format --no-restore

format-check:
	dotnet format --no-restore --verify-no-changes

pack:
	dotnet pack --no-restore --no-build --configuration {{ configuration }} --output {{ package_output_path }}

test:
	dotnet test --no-restore --configuration {{ configuration }}

test-watch:
	dotnet watch test --no-restore --configuration {{ configuration }}

test-filter filter:
	dotnet test --no-restore --configuration {{ configuration }} --filter '{{ filter }}'

test-name name:
	dotnet test --no-restore --configuration {{ configuration }} --filter 'FullyQualifiedName~{{ name }}'

test-scope scope:
	dotnet test --no-restore --configuration {{ configuration }} --filter 'FullyQualifiedName~{{ scope }}'

test-file path:
	dotnet test --no-restore --configuration {{ configuration }} --filter 'FullyQualifiedName~{{ path }}'

test-ci:
	dotnet test \
		--no-restore \
		--no-build \
		--configuration {{ configuration }} \
		--logger trx \
		--results-directory {{ test_results_path }}

list-packages:
	dotnet nuget search HawsLabs --prerelease