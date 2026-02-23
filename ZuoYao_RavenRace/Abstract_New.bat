@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

:: ==================================================================
:: 配置区域
:: ==================================================================

set "output_file=combined_output_selected.txt"
set "defs_features_path=Defs\Features"
set "source_features_path=Source\RavenRace\Features"

:: 【修正点 1】: 将所有要忽略的关键词写在一行，用空格隔开
:: 这里包括了文件夹名和具体的路径片段
set "global_ignore_list=RavenRaceFA RJWCompat AlienRaces Defs\Core\Backstories"

:: ==================================================================
:: 脚本主体
:: ==================================================================

if exist "%output_file%" del "%output_file%"

:: --- 步骤 1: 扫描并构建 Features 菜单 ---
cls
echo.
echo =======================================================
echo  选择要打包的功能模块 (Features)
echo =======================================================
echo.
set "feature_count=0"
set "all_features="
for /d %%D in ("%defs_features_path%\*", "%source_features_path%\*") do (
    set "folder_name=%%~nD"
    echo !all_features! | findstr /I /C:" %%~nD " > nul
    if errorlevel 1 (
        set /a feature_count+=1
        echo   [!feature_count!] %%~nD
        set "feature_folders[!feature_count!]=%%~nD"
        set "all_features=!all_features! %%~nD "
    )
)
echo.
echo -------------------------------------------------------
echo.
echo   (a) 全部打包
echo   (n) 全部跳过
echo.
echo 请输入要打包的模块编号 (多个用空格隔开), 或输入 'a'/'n':
set /p "user_choice="

:: --- 步骤 2: 解析用户选择 ---
set "excluded_feature_paths="
if /i "%user_choice%"=="a" (
    echo.
    echo 选择: 全部打包。
) else if /i "%user_choice%"=="n" (
    echo.
    echo 选择: 全部跳过 Features 文件夹。
    for %%F in (!all_features!) do (
        set "excluded_feature_paths=!excluded_feature_paths! \Features\%%F\ "
    )
) else (
    set "selected_features= "
    for %%C in (%user_choice%) do (
        set "selected_features=!selected_features!!feature_folders[%%C]! "
    )
    for %%F in (!all_features!) do (
        echo !selected_features! | findstr /I /C:" %%F " > nul
        if errorlevel 1 (
            set "excluded_feature_paths=!excluded_feature_paths! \Features\%%F\ "
        )
    )
    echo.
    echo 将跳过包含以下路径的模块:!excluded_feature_paths!
)

echo.
echo 正在汇总文件...
echo.

:: --- 步骤 3: 递归打包文件 ---
(
    for /R . %%F in (*.cs *.xml) do (
        set "file_path=%%F"
        set "should_exclude=false"

        :: 【修正点 2】: 优化后的全局忽略检查逻辑
        :: 只要路径中包含 global_ignore_list 中的任何一个词，就排除
        for %%G in (!global_ignore_list!) do (
            :: 使用字符串替换法检查，比 findstr 快且更稳定
            if not "!file_path:%%G=!" == "!file_path!" (
                set "should_exclude=true"
            )
        )
        
        :: 检查是否在 Features 排除列表中
        if defined excluded_feature_paths (
            for %%E in (!excluded_feature_paths!) do (
                if not "!file_path:%%E=!" == "!file_path!" (
                    set "should_exclude=true"
                )
            )
        )

        :: 如果不排除，则输出
        if "!should_exclude!"=="false" (
            echo =
            echo File: !file_path!
            echo =
            echo.
            type "!file_path!"
            echo.
            echo.
        )
    )
) > "%output_file%"

:: --- 结束 ---
cls
echo.
echo =======================================================
echo  打包完成!
echo =======================================================
echo.
echo  已全局忽略列表: !global_ignore_list!
echo  已跳过的Features模块路径: !excluded_feature_paths!
echo.
echo  结果已保存到: %output_file%
echo.
pause