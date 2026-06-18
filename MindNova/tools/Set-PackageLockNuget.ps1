get-childitem -recurse -Filter '*.csproj' | ForEach-Object {
    $project = $_.FullName # $project = get-childitem -recurse -Filter '*.csproj' | select -skip 2 -first 1
    [xml]$projectData = get-content $project
    $found =  Select-Xml -Xml $projectData -XPath "//RestorePackagesWithLockFile"
    if($found){
        Write-host 'Found'
    }else{
        write-host 'Not Found'
        $element = $projectData.CreateElement('RestorePackagesWithLockFile')
        $element.AppendChild($projectData.CreateTextNode("true"))


        $projectData.Project.PropertyGroup.AppendChild($element)
        $projectData.Save($project)
    }
}