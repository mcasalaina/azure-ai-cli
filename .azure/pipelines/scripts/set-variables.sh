#!/bin/bash

define_variable () {
    echo "$1=$2"
    echo "##vso[task.setvariable variable=$1;isOutput=true]$2"
}

PUBLISH_DEV_BUILD=$1
BUILD_NUMBER=$2
BUILD_ID=$3

echo "Source branch: $BUILD_SOURCEBRANCH"

PUBLISH_BUILD=false

# If the build was triggered from a tag, use the tag as the version. Otherwise, set the version to dev.
REGEX='^refs\/tags\/v?([[:digit:]]+)\.([[:digit:]]+)\.([[:digit:]]+)(-.+)?'

# If tag is a release tag, set up release variables.
if [[ $BUILD_SOURCEBRANCH =~ $REGEX ]]; then
    define_variable "IsRelease" "true"
    PUBLISH_BUILD=true
else
    define_variable "IsRelease" "false"
fi

if [[ $PUBLISH_DEV_BUILD = true ]]; then
    PUBLISH_BUILD=true
fi

# Determine the product version (major.minor.build).
# ref. https://learn.microsoft.com/windows/win32/msi/productversion
# NOTE: If the major or minor version is not updated before a new year, the version number becomes ambiguous
# and it may not be possible to upgrade an old version from the previous year without manual uninstallation.
# Example:
# - last build of year N:    1.0.36599
# - first build of year N+1: 1.0.101   -> cannot update with this, must uninstall 1.0.36599 first.

MAJOR_VERSION="0"
MINOR_VERSION="0"
BUILD_VERSION="0"

# Parse Build.BuildNumber for build date and daily run # (max 99).
if [ ! -z "$BUILD_NUMBER" ]; then
    # e.g. "20240120.2" -> build year 2024, month 01, day 20, run 2
    BUILD_YEAR=$(echo "$BUILD_NUMBER" | sed 's/^\([0-9]\{4\}\)[0-9]\{4\}\.[0-9]*$/\1/')
    BUILD_MONTH=$(echo "$BUILD_NUMBER" | sed 's/^[0-9]\{4\}\([0-9]\{2\}\)[0-9]\{2\}\.[0-9]*$/\1/')
    BUILD_DAY=$(echo "$BUILD_NUMBER" | sed 's/^[0-9]\{6\}\([0-9]\{2\}\)\.[0-9]*$/\1/')
    BUILD_RUN=$(echo "$BUILD_NUMBER" | sed 's/^[0-9]\{8\}\.\([0-9]*$\)/\1/')
    # Remove a potential leading zero to avoid interpretation as an octal number.
    BUILD_MONTH=${BUILD_MONTH#0}
    BUILD_DAY=${BUILD_DAY#0}
    BUILD_RUN=${BUILD_RUN#0}

    if [[ $PUBLISH_BUILD = true ]]; then
        # Set the version for a published build. Note: BUILD_SOURCEBRANCH may override this.
        MAJOR_VERSION="1"
        MINOR_VERSION="0"
        BUILD_VERSION="0"

        if [ ! -z "$BUILD_MONTH" -a $BUILD_MONTH -ge 1 -a $BUILD_MONTH -le 12 -a \
             ! -z "$BUILD_DAY"   -a $BUILD_DAY   -ge 1 -a $BUILD_DAY   -le 31 -a \
             ! -z "$BUILD_RUN"   -a $BUILD_RUN   -ge 1 -a $BUILD_RUN   -le 99 ]
        then
            let DayOfYear="($BUILD_MONTH - 1) * 31 + $BUILD_DAY" # estimate using max days/month
            if [ $BUILD_RUN -lt 10 ]; then
                BUILD_VERSION="${DayOfYear}0${BUILD_RUN}"
            else
                BUILD_VERSION="${DayOfYear}${BUILD_RUN}"
            fi
        else
            >&2 echo "Ignored invalid argument: Build.BuildNumber $BUILD_NUMBER"
        fi
    fi
fi

PRODUCT_VERSION="${MAJOR_VERSION}.${MINOR_VERSION}.${BUILD_VERSION}"
echo "Product version: $PRODUCT_VERSION"

# Append Build.BuildId to version string.
if [ ! -z "$BUILD_ID" ]; then
    DEV_VERSION="${PRODUCT_VERSION}-dev${BUILD_YEAR}.${BUILD_ID}"
else
    DEV_VERSION="${PRODUCT_VERSION}-dev${BUILD_YEAR}"
fi

# Extract version from the tag.
VERSION=$([[ $BUILD_SOURCEBRANCH =~ $REGEX ]] && echo $(echo $BUILD_SOURCEBRANCH | sed -r 's/'$REGEX'/\1.\2.\3\4/') || echo "$DEV_VERSION")

# Set the AICLIVersion variable in the pipeline.
define_variable "AICLIVersion" "$VERSION"

# Set the AICLINuPkgFileName variable in the pipeline.
define_variable "AICLINuPkgFileName" "Azure.AI.CLI.$VERSION.nupkg"

# At this point, the $VERSION may have a pre-release tag. We need to remove it to get the version that will be used for SemVer.
SEMVER_VERSION=$(echo $VERSION | sed -r 's/-.*//')
define_variable "AICLISemVerVersion" "$SEMVER_VERSION"