module ReleaseTasks

open Fake.Core
open Fake.DotNet
open Fake.IO.Globbing.Operators
open BlackFox.Fake
open Fake.Tools

open ProjectInfo
open Helpers

open BasicTasks
open TestTasks
open PackageTasks
