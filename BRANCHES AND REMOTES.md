# Branch name guidance

**Master branch:**  
`master`

**Branches containing work by specific people:**  
`priv/github-user-name.descriptive-name-of-branch`  
`priv/macrogreg.vendor-remotes-instructions`  
`priv/pjanotti.setup-codeowners`  
`. . .`  

**Long-lived branches containing large features that have many commits from multiple authors:**  
`feature/descriptive-name-of-branch`  
`feature/refactor-assembly-structure`  
`feature/instrument-call-targets`  
`. . .`  

**Branches representing the contents of downstream vendor repos:**  
`vendors/vendor-name`    
`vendors/datadog`  
`vendors/splunk`  
`vendors/microsoft`  
`. . .`  

**Branches tracking other repos (you will create them locally, see below):**  
`vendor-remotes/vendor-name`  
`vendor-remotes/datadog`  
`vendor-remotes/splunk`  
`vendor-remotes/microsoft`  
`. . .`   


# Merging from a vendor-repo

## You are merging from a tracking vendor repo into the OTel repo for the first time:

**Create a container folder:**  
```
c:\Code> mkdir DotNET-Instrumentation
```
```
c:\Code> cd DotNET-Instrumentation
```

**Clone this OTel repo:**  
```
c:\Code\DotNET-Instrumentation>git clone git@github.com:open-telemetry/opentelemetry-dotnet-instrumentation.git
```
```
c:\Code\DotNET-Instrumentation>cd opentelemetry-dotnet-instrumentation
```

**Add a remote for your specific vendor repo:**  
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git remote add _VendorName_ _VendorRemoteRepoAddress_
```
for example: 
```
c:\00\Code\GitHubOTel\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git remote add DataDog git@github.com:DataDog/dd-trace-dotnet.git
```

**Validate:**  
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git remote
```
you should see:
```
DataDog
origin
```

**Fetch branches from the new remote:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git fetch DataDog
```

**Create a branch tracking your remote:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout -b vendor-remotes/_vendor-name_ --track _VendorName_/master
```
for example
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout -b vendor-remotes/datadog --track DataDog/master
```

You should see:
```
Switched to a new branch 'vendor-remotes/datadog'
Branch 'vendor-remotes/datadog' set up to track remote branch 'master' from 'DataDog'.
```

**Create a new branch with the same contents as the one you just created, but tracking the OTel origin (please mind the naming convention):**  
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout -b vendors/_vendor-name_
```
for example:
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout -b vendors/datadog
```

**Merge from master into vendors/vendor-name to create a common baseline:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git merge master --allow-unrelated-histories
```

**Push to OTel repo:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git push --set-upstream origin vendors/_vendor-name_
```
e.g.:
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git push --set-upstream origin vendors/datadog
```

Now you can **go to the GitHub site** and **create a pull request from "vendors/_vendor-name_" into "master"**.

## You have previously merged from a tracking vendor repo into the OTel repo

* You already have a remote named "_VendorName_" and a local branch named "vendor-remotes/_vendor-name_" which tracks thar remove.  
If not, see above.

* Pull your "vendor-remotes/_vendor-name_"_ branch.

* Merge your local branch "vendor-remotes/_vendor-name_" which tracks the vendor remote into your local branch "vendors/_vendor-name_" which tracks the OTel remote (origin).

* Push your “vendors/vendor-name” branch.

* Create a PR from “vendors/vendor-name” Into “master”.