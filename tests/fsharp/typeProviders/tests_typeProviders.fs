module ``FSharp-Tests-TypeProviders``

open System
open System.IO
open NUnit.Framework

open FSharpTestSuiteTypes
open NUnitConf
open PlatformHelpers

let setTestDataInfo name = FSharpTestSuite.setTestDataInfo ("typeProviders", name)

let testContext () =
    { Directory = NUnit.Framework.TestContext.CurrentContext.Test.Properties.["DIRECTORY"] :?> string;
      Config = suiteHelpers.Value }

let requireVSUltimate cfg = processor {
    do! match cfg.INSTALL_SKU with
        | Some (Ultimate) -> Success
        | x ->
            // IF /I "%INSTALL_SKU%" NEQ "ULTIMATE" (
            //     echo Test not supported except on Ultimate
            NUnitConf.skip (sprintf "Test not supported except on Ultimate, was %A" x)
            //     exit /b 0
            // )
    }

module Builtin =

    module EdmxFile = 
        let permutations = 
            FSharpTestSuite.allPermutation
            |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo ("builtin"/"EdmxFile"))

        [<Test; TestCaseSource("permutations")>]
        let EdmxFile p = check (processor {
            let { Directory = dir; Config = cfg } = testContext ()
        
            //call %~d0%~p0..\copyFSharpDataTypeProviderDLL.cmd
            do! CopyFSharpDataTypeProviderDLL.copy cfg dir

            do! SingleTestBuild.singleTestBuild cfg dir p
        
            do! SingleTestRun.singleTestRun cfg dir p
            })


    module ODataService = 
        let permutations = 
            FSharpTestSuite.allPermutation
            |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo ("builtin"/"ODataService"))

        [<Test; TestCaseSource("permutations")>]
        let oDataService p = check (processor {
            let { Directory = dir; Config = cfg } = testContext ()
        
            //call %~d0%~p0..\copyFSharpDataTypeProviderDLL.cmd
            do! CopyFSharpDataTypeProviderDLL.copy cfg dir
        
            do! SingleTestBuild.singleTestBuild cfg dir p
        
            do! SingleTestRun.singleTestRun cfg dir p
            })


    module SqlDataConnection = 
        let permutations = 
            FSharpTestSuite.allPermutation
            |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo ("builtin"/"SqlDataConnection"))

        [<Test; TestCaseSource("permutations")>]
        let sqlDataConnection p = check (processor {
            let { Directory = dir; Config = cfg } = testContext ()

            let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
            let fileExists = Commands.fileExists dir >> Option.isSome
        
            //call %~d0%~p0..\copyFSharpDataTypeProviderDLL.cmd
            do! CopyFSharpDataTypeProviderDLL.copy cfg dir
        
            do! SingleTestBuild.singleTestBuild cfg dir p

            // IF /I "%INSTALL_SKU%" NEQ "ULTIMATE" (
            //     echo Test not supported except on Ultimate
            //     exit /b 0
            // )
            do! requireVSUltimate cfg

            // IF EXIST test.exe (
            //    echo Running test.exe to warm up SQL
            //    test.exe > nul 2> nul
            // )
            do! if fileExists "test.exe"
                then
                    // echo Running test.exe to warm up SQL
                    // test.exe > nul 2> nul
                    exec ("."/"test.exe") ""
                else Success ()
        
            do! SingleTestRun.singleTestRun cfg dir p
            })

    module WsdlService = 
        let permutations = 
            FSharpTestSuite.allPermutation
            |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo ("builtin"/"WsdlService"))

        [<Test; TestCaseSource("permutations")>]
        let wsdlService p = check (processor {
            let { Directory = dir; Config = cfg } = testContext ()
        
            //call %~d0%~p0..\copyFSharpDataTypeProviderDLL.cmd
            do! CopyFSharpDataTypeProviderDLL.copy cfg dir
        
            do! SingleTestBuild.singleTestBuild cfg dir p
        
            do! SingleTestRun.singleTestRun cfg dir p
            })



[<Category("fail_new"); Category("fail_old")>]
module DiamondAssembly = 

    let build cfg dir = processor {

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let fsc = Printf.ksprintf (Commands.fsc exec cfg.FSC)
        let del = Commands.rm dir

        // if EXIST provider.dll del provider.dll
        del "provider.dll"

        // "%FSC%" --out:provided.dll -a ..\helloWorld\provided.fs
        do! fsc "%s" "--out:provided.dll -a" [".."/"helloWorld"/"provided.fs"]

        // "%FSC%" --out:provider.dll -a ..\helloWorld\provider.fsx
        do! fsc "%s" "--out:provider.dll -a" [".."/"helloWorld"/"provider.fsx"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test1.dll -a test1.fsx
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test1.dll -a" cfg.fsc_flags ["test1.fsx"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test2a.dll -a -r:test1.dll test2a.fsx
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test2a.dll -a -r:test1.dll" cfg.fsc_flags ["test2a.fsx"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test2b.dll -a -r:test1.dll test2b.fsx
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test2b.dll -a -r:test1.dll" cfg.fsc_flags ["test2b.fsx"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test3.exe -r:test1.dll -r:test2a.dll -r:test2b.dll test3.fsx
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test3.exe -r:test1.dll -r:test2a.dll -r:test2b.dll" cfg.fsc_flags ["test3.fsx"]

        }

    let run cfg dir = processor {

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let peverify = Commands.peverify exec cfg.PEVERIFY
        let fsi = Printf.ksprintf (Commands.fsi exec cfg.FSI)
        let fileguard = (Commands.getfullpath dir) >> FileGuard.create

        // "%PEVERIFY%" test1.dll
        do! peverify "test1.dll"

        // "%PEVERIFY%" test2a.dll
        do! peverify "test2a.dll"

        // "%PEVERIFY%" test2b.dll
        do! peverify "test2b.dll"

        // "%PEVERIFY%" test3.exe
        do! peverify "test3.exe"

        // test3.exe
        do! exec ("."/"test3.exe") ""



        // if exist test.ok (del /f /q test.ok)
        use testOkFile = fileguard "test.ok"

        // %CLIX% "%FSI%" %fsi_flags% test1.fsx test2a.fsx test2b.fsx test3.fsx && (
        do! fsi "%s" cfg.fsi_flags ["test1.fsx"; "test2a.fsx"; "test2b.fsx"; "test3.fsx"]

        // dir test.ok > NUL 2>&1 ) || (
        // @echo :FSI load failed
        // set ERRORMSG=%ERRORMSG% FSI load failed;
        do! testOkFile |> NUnitConf.checkGuardExists
        // )

        }

    let testData = [ (new TestCaseData()) |> setTestDataInfo "diamondAssembly" ]

    [<Test; TestCaseSource("testData")>]
    let diamondAssembly () = check (processor {
        let { Directory = dir; Config = cfg } = testContext ()

        do! build cfg dir

        do! run cfg dir
                
        })



module GlobalNamespace = 

    let testData = [ (new TestCaseData()) |> setTestDataInfo "globalNamespace" ]

    [<Test; TestCaseSource("testData")>]
    let globalNamespace () = check (processor {
        let { Directory = dir; Config = cfg } = testContext ()

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let fsc = Printf.ksprintf (Commands.fsc exec cfg.FSC)
        let csc = Printf.ksprintf (Commands.csc exec cfg.CSC)

        // %CSC% /out:globalNamespaceTP.dll /debug+ /target:library /r:"%FSCOREDLLPATH%" globalNamespaceTP.cs
        do! csc """/out:globalNamespaceTP.dll /debug+ /target:library /r:"%s" """ cfg.FSCOREDLLPATH ["globalNamespaceTP.cs"]

        // "%FSC%" %fsc_flags% /debug+ /r:globalNamespaceTP.dll /optimize- test.fsx
        do! fsc "%s /debug+ /r:globalNamespaceTP.dll /optimize-" cfg.fsc_flags ["test.fsx"]
                
        })


module HelloWorld = 

    let build cfg dir p = processor {

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let fsc = Printf.ksprintf (Commands.fsc exec cfg.FSC)
        let peverify = Commands.peverify exec cfg.PEVERIFY
        let del = Commands.rm dir
        let execIn workDir p = Command.exec workDir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let fsc' execIn = Printf.ksprintf (Commands.fsc execIn cfg.FSC)
        let mkdir = Commands.mkdir_p dir
        let getfullpath = Commands.getfullpath dir

        //if EXIST provided.dll del provided.dll
        del "provided.dll"

        //if EXIST provided1.dll del provided1.dll
        del "provided1.dll"

        //"%FSC%" --out:provided1.dll -g -a ..\helloWorld\provided.fs
        do! fsc "%s" "--out:provided1.dll -g -a" [".."/"helloWorld"/"provided.fs"]

        //if EXIST provided2.dll del provided2.dll
        del "provided2.dll"

        //"%FSC%" --out:provided2.dll -g -a ..\helloWorld\provided.fs
        do! fsc "%s" "--out:provided2.dll -g -a" [".."/"helloWorld"/"provided.fs"]

        //if EXIST provided3.dll del provided3.dll
        del "provided3.dll"

        //"%FSC%" --out:provided3.dll -g -a ..\helloWorld\provided.fs
        do! fsc "%s" "--out:provided3.dll -g -a" [".."/"helloWorld"/"provided.fs"]

        //if EXIST provided4.dll del provided4.dll
        del "provided4.dll"

        //"%FSC%" --out:provided4.dll -g -a ..\helloWorld\provided.fs
        do! fsc "%s" "--out:provided4.dll -g -a" [".."/"helloWorld"/"provided.fs"]

        //if EXIST providedJ.dll del providedJ.dll
        del "providedJ.dll"

        //"%FSC%" --out:providedJ.dll -g -a ..\helloWorld\providedJ.fs
        do! fsc "%s" "--out:providedJ.dll -g -a" [".."/"helloWorld"/"providedJ.fs"]

        //if EXIST providedK.dll del providedK.dll
        del "providedK.dll"

        //"%FSC%" --out:providedK.dll -g -a ..\helloWorld\providedK.fs
        do! fsc "%s" "--out:providedK.dll -g -a" [".."/"helloWorld"/"providedK.fs"]

        //"%FSC%" --out:providedNullAssemblyName.dll -g -a ..\helloWorld\providedNullAssemblyName.fsx
        do! fsc "%s" "--out:providedNullAssemblyName.dll -g -a" [".."/"helloWorld"/"providedNullAssemblyName.fsx"]

        //call %~d0%~p0\..\build-typeprovider-test.bat
        do! BuildTypeProviderTest.build cfg dir p

        //if EXIST provider_with_binary_compat_changes.dll del provider_with_binary_compat_changes.dll
        del "provider_with_binary_compat_changes.dll"

        //mkdir bincompat1
        mkdir "bincompat1"

        //pushd bincompat1
        log "pushd bincompat1"
        let bincompat1 = getfullpath "bincompat1"

        //xcopy /y ..\*.dll .
        Directory.EnumerateFiles(bincompat1/"..", "*.dll")
        |> Seq.iter (fun from -> Commands.copy_y bincompat1 from ("."/Path.GetFileName(from)) |> ignore)

        //"%FSC%" -g -a -o:test_lib.dll -r:provider.dll ..\test.fsx
        do! fsc' (execIn bincompat1) "%s" "-g -a -o:test_lib.dll -r:provider.dll" [".."/"test.fsx"]

        //"%FSC%" -r:test_lib.dll -r:provider.dll ..\testlib_client.fsx
        do! fsc' (execIn bincompat1) "%s" "-r:test_lib.dll -r:provider.dll" [".."/"testlib_client.fsx"]

        //popd
        log "popd"

        //mkdir bincompat2
        mkdir "bincompat2"
        
        //pushd bincompat2
        log "pushd bincompat2"
        let bincompat2 = getfullpath "bincompat2"

        //xcopy /y ..\bincompat1\*.dll .
        Directory.EnumerateFiles(bincompat2/".."/"bincompat1", "*.dll")
        |> Seq.iter (fun from -> Commands.copy_y bincompat2 from ("."/Path.GetFileName(from)) |> ignore)


        //REM overwrite provider.dll
        //"%FSC%" --define:ADD_AN_OPTIONAL_STATIC_PARAMETER --out:provider.dll -g -a ..\provider.fsx
        do! fsc' (execIn bincompat2) "%s" "--define:ADD_AN_OPTIONAL_STATIC_PARAMETER --out:provider.dll -g -a" [".."/"provider.fsx"]

        //REM This is the important part of the binary compatibility part of the test: the new provider is being used, but 
        //REM with a binary that was generated w.r.t. the old provider. The new provider can still resolve the references
        //REM generated by the old provider which are stored in the F# metadata for test_lib.dll
        //"%FSC%" --define:ADD_AN_OPTIONAL_STATIC_PARAMETER -r:test_lib.dll -r:provider.dll ..\testlib_client.fsx
        do! fsc' (execIn bincompat2) "%s" "--define:ADD_AN_OPTIONAL_STATIC_PARAMETER -r:test_lib.dll -r:provider.dll" [".."/"testlib_client.fsx"]

        //"%PEVERIFY%" provider.dll
        do! peverify (bincompat2/"provider.dll")

        //"%PEVERIFY%" test_lib.dll
        do! peverify (bincompat2/"test_lib.dll")

        //"%PEVERIFY%" testlib_client.exe
        do! peverify (bincompat2/"testlib_client.exe")

        }

    let permutations = 
        FSharpTestSuite.allPermutation
        |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo "helloWorld")

    [<Test; TestCaseSource("permutations")>]
    let helloWorld p = check (processor {
        let { Directory = dir; Config = cfg } = testContext ()

        do! build cfg dir p

        do! SingleTestRun.singleTestRun cfg dir p
                
        })



module HelloWorldCSharp = 

    let build cfg dir = processor {

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let fsc = Printf.ksprintf (Commands.fsc exec cfg.FSC)
        let csc = Printf.ksprintf (Commands.csc exec cfg.CSC)
        let del = Commands.rm dir
        let gacutil = Commands.gacutil exec cfg.GACUTIL

        // if EXIST magic.dll del magic.dll
        del "magic.dll"

        // "%FSC%" --out:magic.dll -a magic.fs --keyfile:magic.snk
        do! fsc "%s" "--out:magic.dll -a --keyfile:magic.snk" ["magic.fs "]

        // REM == If we are running this test on a lab machine, we may not be running from an elev cmd prompt
        // REM == In that case, ADMIN_PIPE is set to the tool to invoke the command elevated.
        // IF DEFINED ADMIN_PIPE %ADMIN_PIPE% %GACUTIL% /if magic.dll
        
        //REVIEW check ADMIN_PIPE and elevated gac
        ignore "useless ADMIN_PIPE, test are run as administrator"

        // if EXIST provider.dll del provider.dll
        del "provider.dll"

        // %CSC% /out:provider.dll /target:library "/r:%FSCOREDLLPATH%" /r:magic.dll provider.cs
        do! csc """/out:provider.dll /target:library "/r:%s" /r:magic.dll""" cfg.FSCOREDLLPATH ["provider.cs"]

        // "%GACUTIL%" /if magic.dll
        do! gacutil "/if" "magic.dll"

        // "%FSC%" %fsc_flags% /debug+ /r:provider.dll /optimize- test.fsx
        do! fsc "%s /debug+ /r:provider.dll /optimize-" cfg.fsc_flags ["test.fsx"]

        }

    let run cfg dir = processor {

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let peverify = Commands.peverify exec cfg.PEVERIFY

        // "%PEVERIFY%" magic.dll
        do! peverify "magic.dll"

        // "%PEVERIFY%" provider.dll
        do! peverify "provider.dll"

        // "%PEVERIFY%" test.exe
        do! peverify "test.exe"

        // test.exe
        do! exec ("."/"test.exe") ""

        }

    let testData = [ (new TestCaseData()) |> setTestDataInfo "helloWorldCSharp" ]

    [<Test; TestCaseSource("testData")>]
    let helloWorldCSharp () = check (processor {
        let { Directory = dir; Config = cfg } = testContext ()

        do! build cfg dir

        do! run cfg dir
                
        })



[<Category("fail_new"); Category("fail_old"); >] 
module NegTests = 

    let testData = 
        // set TESTS_SIMPLE=neg2h neg4 neg1 neg1_a neg2 neg2c neg2e neg2g neg6
        let testsSimple = 
            ["neg2h"; "neg4"; "neg1"; "neg1_a"; "neg2"; "neg2c"; "neg2e"; "neg2g"; "neg6"]
        // REM neg7 - excluded 
        // set TESTS_SIMPLE=%TESTS_SIMPLE% InvalidInvokerExpression providerAttributeErrorConsume ProviderAttribute_EmptyConsume
            @ ["InvalidInvokerExpression"; "providerAttributeErrorConsume"; "ProviderAttribute_EmptyConsume"]
        
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetNestedNamespaces_Exception
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_NamespaceName_Exception
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_NamespaceName_Empty
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetTypes_Exception
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_ResolveTypeName_Exception
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetNamespaces_Exception
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetStaticParameters_Exception 
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetInvokerExpression_Exception 
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetTypes_Null
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_ResolveTypeName_Null
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetNamespaces_Null
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetStaticParameters_Null
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_GetInvokerExpression_Null
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_DoesNotHaveConstructor
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_ConstructorThrows
        // set TESTS_WITH_DEFINE=%TESTS_WITH_DEFINE% EVIL_PROVIDER_ReturnsTypeWithIncorrectNameFromApplyStaticArguments
        let testsWithDefine = [
            "EVIL_PROVIDER_GetNestedNamespaces_Exception";
            "EVIL_PROVIDER_NamespaceName_Exception";
            "EVIL_PROVIDER_NamespaceName_Empty";
            "EVIL_PROVIDER_GetTypes_Exception";
            "EVIL_PROVIDER_ResolveTypeName_Exception";
            "EVIL_PROVIDER_GetNamespaces_Exception";
            "EVIL_PROVIDER_GetStaticParameters_Exception";
            "EVIL_PROVIDER_GetInvokerExpression_Exception";
            "EVIL_PROVIDER_GetTypes_Null";
            "EVIL_PROVIDER_ResolveTypeName_Null";
            "EVIL_PROVIDER_GetNamespaces_Null";
            "EVIL_PROVIDER_GetStaticParameters_Null";
            "EVIL_PROVIDER_GetInvokerExpression_Null";
            "EVIL_PROVIDER_DoesNotHaveConstructor";
            "EVIL_PROVIDER_ConstructorThrows";
            "EVIL_PROVIDER_ReturnsTypeWithIncorrectNameFromApplyStaticArguments" ]
        
        (testsSimple @ testsWithDefine)
        |> List.map (fun name -> (new TestCaseData(name)) |> setTestDataInfo "negTests" )

    [<Test; TestCaseSource("testData")>]
    let negTests name = check (processor {
        let { Directory = dir; Config = cfg } = testContext ()

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let fsc = Commands.fsc exec cfg.FSC
        let del = Commands.rm dir
        let fileExists = Commands.fileExists dir >> Option.isSome
        let getfullpath = Commands.getfullpath dir

        // if EXIST provided.dll del provided.dll
        del "provided.dll"

        // "%FSC%" --out:provided.dll -a ..\helloWorld\provided.fs
        do! fsc "--out:provided.dll -a" [".."/"helloWorld"/"provided.fs"]

        // if EXIST providedJ.dll del providedJ.dll
        del "providedJ.dll"

        // "%FSC%" --out:providedJ.dll -a ..\helloWorld\providedJ.fs
        do! fsc "--out:providedJ.dll -a" [".."/"helloWorld"/"providedJ.fs"]

        // if EXIST providedK.dll del providedK.dll
        del "providedK.dll"

        // "%FSC%" --out:providedK.dll -a ..\helloWorld\providedK.fs
        do! fsc "--out:providedK.dll -a" [".."/"helloWorld"/"providedK.fs"]

        // if EXIST provider.dll del provider.dll
        del "provider.dll"

        // "%FSC%" --out:provider.dll -a  provider.fsx
        do! fsc "--out:provider.dll -a" ["provider.fsx"]

        // "%FSC%" --out:provider_providerAttributeErrorConsume.dll -a  providerAttributeError.fsx
        do! fsc "--out:provider_providerAttributeErrorConsume.dll -a" ["providerAttributeError.fsx"]

        // "%FSC%" --out:provider_ProviderAttribute_EmptyConsume.dll -a  providerAttribute_Empty.fsx
        do! fsc "--out:provider_ProviderAttribute_EmptyConsume.dll -a" ["providerAttribute_Empty.fsx"]

        // if EXIST helloWorldProvider.dll del helloWorldProvider.dll
        del "helloWorldProvider.dll"

        // "%FSC%" --out:helloWorldProvider.dll -a  ..\helloWorld\provider.fsx
        do! fsc "--out:helloWorldProvider.dll -a" [".."/"helloWorld"/"provider.fsx"]

        // if EXIST MostBasicProvider.dll del MostBasicProvider.dll
        del "MostBasicProvider.dll"

        // "%FSC%" --out:MostBasicProvider.dll -a  MostBasicProvider.fsx
        do! fsc "--out:MostBasicProvider.dll -a" ["MostBasicProvider.fsx"]

        //REVIEW use testfixture setup to run this code ---^ only once?

        // if "%1"=="" goto :RunAllTests
        // if "%1"=="--withDefine" goto :RunSpecificWithDefine
        // call :RunTest %1
        // goto :ReportResults
        ignore "is a parametrized test, like --withDefine"

        // :Preprocess
        let preprocess bslppName pref = processor {

            let tempFile = Path.GetTempFileName()

            let ``exec <`` l p = Command.exec dir cfg.EnvironmentVariables { Output = Output(Overwrite(tempFile)); Input = Some(RedirectInput(l)) } p >> checkResult
            let ``| exec >`` out p = Command.exec dir cfg.EnvironmentVariables { Output = Output(Overwrite(out)); Input = Some(RedirectInput(tempFile)) } p >> checkResult

            let ``fsi <`` = Printf.ksprintf (fun flags l -> Commands.fsi (``exec <`` l) cfg.FSI flags [])
            let ``| fsi >`` = Printf.ksprintf (fun flags sources out -> Commands.fsi (``| exec >`` out) cfg.FSI flags sources)

            // "%FSI%" --exec sed.fsx "<ASSEMBLY>" "%~d0%~p0provider_%1.dll" < %~1.%~2bslpp 
            do! ``fsi <`` """--exec sed.fsx "<ASSEMBLY>" "%s" """ (getfullpath (sprintf "provider_%s.dll" name)) (sprintf "%s.%sbslpp" bslppName pref) 

            // | fsi --exec sed.fsx "<URIPATH>" "file:///%CD%\\" > %~1.%~2bsl
            do! ``| fsi >`` """--exec sed.fsx "<URIPATH>" "%O" """ (Uri(dir |> Commands.pathAddBackslash)) [] (sprintf "%s.%sbsl" bslppName pref)
            }

        // :RunTestWithDefine
        let runTestWithDefine = processor {
            // "%FSC%" --define:%1 --out:provider_%1.dll -a  provider.fsx
            do! fsc (sprintf "--define:%s --out:provider_%s.dll -a" name name) ["provider.fsx"]

            // :RunTest
            // if EXIST %1.bslpp   call :Preprocess "%1" ""
            do! if fileExists (sprintf "%s.bslpp" name)
                then preprocess name ""
                else Success

            // if EXIST %1.vsbslpp call :Preprocess "%1" "vs"
            do! if fileExists (sprintf "%s.vsbslpp" name)
                then preprocess name "vs"
                else Success

            // :DoRunTest
            // call ..\..\single-neg-test.bat %1
            do! SingleNegTest.singleNegTest cfg dir name

            }

        // :RunSpecificWithDefine
        // call :RunTestWithDefine %2
        do! runTestWithDefine
        // goto :ReportResults
        ignore "useless, checked already"

        // :RunAllTests
        // for %%T in (%TESTS_SIMPLE%) do call :RunTest %%T
        // for %%T in (%TESTS_WITH_DEFINE%) do call :RunTestWithDefine %%T
        ignore "is a parametrized test"

                
        })


module SplitAssembly = 
    let permutations = 
        FSharpTestSuite.allPermutation
        |> List.map (fun p -> (new TestCaseData (p)).SetCategory(sprintf "%A" p) |> setTestDataInfo "splitAssembly")

    [<Test; TestCaseSource("permutations")>]
    let splitAssembly p = check (processor {
        let { Directory = dir; Config = cfg } = testContext ()

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let fsc = Printf.ksprintf (Commands.fsc exec cfg.FSC)

        // "%FSC%" --out:provider.dll -a provider.fs
        do! fsc "--out:provider.dll -a" ["provider.fs"]

        // "%FSC%" --out:providerDesigner.dll -a providerDesigner.fsx
        do! fsc "--out:providerDesigner.dll -a" ["providerDesigner.fsx"]

        do! SingleTestBuild.singleTestBuild cfg dir p
        
        do! SingleTestRun.singleTestRun cfg dir p
        })



module WedgeAssembly = 

    let build cfg dir = processor {

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let fsc = Printf.ksprintf (Commands.fsc exec cfg.FSC)
        let del = Commands.rm dir

        // if EXIST provider.dll del provider.dll
        del "provider.dll"

        // if EXIST provided.dll del provided.dll
        del "provided.dll"

        // "%FSC%" --out:provided.dll -a ..\helloWorld\provided.fs
        do! fsc "%s" "--out:provided.dll -a" [".."/"helloWorld"/"provided.fs"]

        // if EXIST providedJ.dll del providedJ.dll
        del "providedJ.dll"

        // "%FSC%" --out:providedJ.dll -a ..\helloWorld\providedJ.fs
        do! fsc "%s" "--out:providedJ.dll -a" [".."/"helloWorld"/"providedJ.fs"]

        // if EXIST providedK.dll del providedK.dll
        del "providedK.dll"

        // "%FSC%" --out:providedK.dll -a ..\helloWorld\providedK.fs
        do! fsc "%s" "--out:providedK.dll -a" [".."/"helloWorld"/"providedK.fs"]

        // "%FSC%" --out:provider.dll -a ..\helloWorld\provider.fsx
        do! fsc "%s" "--out:provider.dll -a" [".."/"helloWorld"/"provider.fsx"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test2a.dll -a test2a.fs
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test2a.dll -a" cfg.fsc_flags ["test2a.fs"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test2b.dll -a test2b.fs
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test2b.dll -a" cfg.fsc_flags ["test2b.fs"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test3.exe test3.fsx
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test3.exe" cfg.fsc_flags ["test3.fsx"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test2a-with-sig.dll -a test2a.fsi test2a.fs
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test2a-with-sig.dll -a" cfg.fsc_flags ["test2a.fsi"; "test2a.fs"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test2b-with-sig.dll -a test2b.fsi test2b.fs
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test2b-with-sig.dll -a" cfg.fsc_flags ["test2b.fsi"; "test2b.fs"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test3-with-sig.exe --define:SIGS test3.fsx
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test3-with-sig.exe --define:SIGS" cfg.fsc_flags ["test3.fsx"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test2a-with-sig-restricted.dll -a test2a-restricted.fsi test2a.fs
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test2a-with-sig-restricted.dll -a" cfg.fsc_flags ["test2a-restricted.fsi"; "test2a.fs"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test2b-with-sig-restricted.dll -a test2b-restricted.fsi test2b.fs
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test2b-with-sig-restricted.dll -a"cfg.fsc_flags ["test2b-restricted.fsi"; "test2b.fs"]

        // "%FSC%" %fsc_flags% --debug+ -r:provider.dll --optimize- -o:test3-with-sig-restricted.exe --define:SIGS_RESTRICTED test3.fsx
        do! fsc "%s --debug+ -r:provider.dll --optimize- -o:test3-with-sig-restricted.exe --define:SIGS_RESTRICTED" cfg.fsc_flags ["test3.fsx"]

        }

    let run cfg dir = processor {

        let exec p = Command.exec dir cfg.EnvironmentVariables { Output = Inherit; Input = None; } p >> checkResult
        let peverify = Commands.peverify exec cfg.PEVERIFY

        // "%PEVERIFY%" test2a.dll
        do! peverify "test2a.dll"

        // "%PEVERIFY%" test2b.dll
        do! peverify "test2b.dll"

        // "%PEVERIFY%" test3.exe
        do! peverify "test3.exe"

        // test3.exe
        do! exec ("."/"test3.exe") ""

        }

    let testData = [ (new TestCaseData()) |> setTestDataInfo "wedgeAssembly" ]

    [<Test; TestCaseSource("testData")>]
    let wedgeAssembly () = check (processor {
        let { Directory = dir; Config = cfg } = testContext ()

        do! build cfg dir

        do! run cfg dir
                
        })
