#!/bin/bash

echo "Source branch: $BUILD_SOURCEBRANCH"

# If the build was triggered from a tag, use the tag as the version. Otherwise, set the version to dev.
REGEX='^refs\/tags\/v?([[:digit:]]+)\.([[:digit:]]+)\.([[:digit:]]+)(-.+)?'

# If tag is a release tag, set up release variables.
if [[ $BUILD_SOURCEBRANCH =~ $REGEX ]]; then
    echo "##vso[task.setvariable variable=ShouldCreateRelease; isOutput=true;]true"

    # Extract version from the tag.
    VERSION=$(echo $BUILD_SOURCEBRANCH | sed -r 's/'$REGEX'/\1.\2.\3\4/')

    # Set the AICLIVersion variable in the pipeline.
    echo "##vso[task.setvariable variable=AICLIVersion; isOutput=true;]$VERSION"

    # Set the AICLINuPkgFileName variable in the pipeline.
    echo "##vso[task.setvariable variable=AICLINuPkgFileName; isOutput=true;]Azure.AI.CLI.$VERSION.nupkg"
else
    echo "##vso[task.setvariable variable=ShouldCreateRelease; isOutput=true;]false"
fi
