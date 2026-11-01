#!/usr/bin/env bash
# Compile check OUTSIDE Unity using Mono's mcs and hand-written API stubs, then run the pure-logic tests.
# Usage (from repo root):  bash Tools/CompileCheck/run.sh
# Requires: mono-mcs + mono-runtime  (apt-get install mono-mcs mono-runtime)
#
# What this proves:   our C# parses, type-checks against itself, and the pure logic tests pass.
# What it does NOT:   that UnityEngine/UnityEditor API signatures match Unity 6 - only the Editor proves that.
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=Tools/CompileCheck/bin
mkdir -p "$OUT"
STUBS="Tools/CompileCheck/Stubs/UnityEngineStubs.cs Tools/CompileCheck/Stubs/UnityEditorStubs.cs"
NUNIT="Tools/CompileCheck/Stubs/NUnitStubs.cs"
RUNTIME=$(find Assets/WonderJump/Runtime -name '*.cs' | sort)
EDITOR=$(find Assets/WonderJump/Editor -name '*.cs' | sort)
EDITMODE=$(find Assets/WonderJump/Tests/EditMode -name '*.cs' | sort)
PLAYMODE=$(find Assets/WonderJump/Tests/PlayMode -name '*.cs' | sort)
FLAGS="-nologo -warnaserror- -nowarn:0414,0649,0169 -define:UNITY_EDITOR -define:UNITY_INCLUDE_TESTS"

echo "== 1/4 Runtime assembly (no editor define)"
mcs -nologo -target:library -out:$OUT/Runtime.dll -nowarn:0414,0649 $STUBS $RUNTIME
echo "== 2/4 Runtime + Editor"
mcs $FLAGS -target:library -out:$OUT/Editor.dll $STUBS $RUNTIME $EDITOR
echo "== 3/4 Runtime + PlayMode tests"
mcs $FLAGS -target:library -out:$OUT/PlayMode.dll $STUBS $NUNIT $RUNTIME $PLAYMODE
echo "== 4/4 Runtime + Editor + EditMode tests + runner"
mcs $FLAGS -target:exe -out:$OUT/EditModeRunner.exe $STUBS $NUNIT $RUNTIME $EDITOR $EDITMODE Tools/CompileCheck/TestRunner.cs
echo "== Running pure-logic EditMode tests"
mono $OUT/EditModeRunner.exe
