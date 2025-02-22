#!/bin/sh

# Copyright (c) Citrix Systems Inc. 
# All rights reserved.
# 
# Redistribution and use in source and binary forms, 
# with or without modification, are permitted provided 
# that the following conditions are met: 
# 
# *   Redistributions of source code must retain the above 
#     copyright notice, this list of conditions and the 
#     following disclaimer. 
# *   Redistributions in binary form must reproduce the above 
#     copyright notice, this list of conditions and the 
#     following disclaimer in the documentation and/or other 
#     materials provided with the distribution. 
# 
# THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
# CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
# INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
# MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
# DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
# CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
# SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
# BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
# SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
# INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
# WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
# NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
# OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
# SUCH DAMAGE.

# This is usually called by Jenkins
# If you define DEBUG environment variable you can make it more verbose.
#
# When are you doing modifications to the build system, always do them in such
# way that it will continue to work even if it's executed manually by a developer
# or from a build automation system.

function check_deps ()
{
  local -a MISSING=()
  for DEP in "$@"
  do
    which $DEP >/dev/null 2>&1
    if [ $? -ne 0 ]; then
      MISSING[${#MISSING}]=${DEP}
    fi
  done
  if [ ${#MISSING} -gt 0 ]; then
    echo "FATAL: One or more build tools were not found in PATH: ${MISSING[@]}"
    exit 1
  fi
}

check_deps nunit-console.exe zip unzip wget curl hg git patch mt.exe candle.exe light.exe
if [ "${BUILD_KIND:+$BUILD_KIND}" != production ]
then
    check_deps signtool.exe
fi


if [ -v DEBUG ];
then
  echo "INFO:	DEBUG mode activated (verbose)"
  set -x
fi

set -e

XENADMIN_DIR="$( cd -P "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"

source ${XENADMIN_DIR}/mk/declarations.sh

#create manifest
echo "@branch=${XS_BRANCH}" >> ${OUTPUT_DIR}/manifest
echo "xenadmin xenadmin.git ${get_REVISION:0:12}" >> ${OUTPUT_DIR}/manifest

if test -z "${XC_BRANDING}"; then XC_BRANDING=citrix; fi

rm -rf ${ROOT}/xenadmin-branding.git
BRAND_REMOTE=https://code.citrite.net/scm/xsc/xenadmin-branding.git

if [ -z $(git ls-remote --heads ${BRAND_REMOTE} | grep ${XS_BRANCH}) ] ; then
    echo "Branch ${XS_BRANCH} not found on xenadmin-branding.git. Reverting to master."
    git clone -b master ${BRAND_REMOTE} ${ROOT}/xenadmin-branding.git
else
    git clone -b ${XS_BRANCH} ${BRAND_REMOTE} ${ROOT}/xenadmin-branding.git
fi

XENADMIN_BRANDING_TIP=$(cd ${ROOT}/xenadmin-branding.git && git rev-parse HEAD)
echo "xenadmin-branding xenadmin-branding.git ${XENADMIN_BRANDING_TIP}" >> ${OUTPUT_DIR}/manifest

if [ -d ${ROOT}/xenadmin-branding.git/${XC_BRANDING} ]; then
    echo "Overwriting Branding folder"
    rm -rf ${XENADMIN_DIR}/Branding/*
    cp -rf ${ROOT}/xenadmin-branding.git/${XC_BRANDING}/* ${XENADMIN_DIR}/Branding/
fi

test_phase()
{
  # Skip the tests if the SKIP_TESTS variable is defined (e.g. in the Jenkins UI, add "export SKIP_TESTS=1" above the call for build script)
  if [ -n "${SKIP_TESTS+x}" ]; then
    echo "Tests skipped because SKIP_TESTS declared"
  else
    source ${XENADMIN_DIR}/mk/tests-checks.sh
  fi
}

production_jenkins_build()
{
    source ${XENADMIN_DIR}/mk/bumpBuildNumber.sh
    source ${XENADMIN_DIR}/devtools/check-roaming.sh
    source ${XENADMIN_DIR}/devtools/i18ncheck/i18ncheck.sh
    source ${XENADMIN_DIR}/devtools/deadcheck/deadcheck.sh
    source ${XENADMIN_DIR}/devtools/spellcheck/spellcheck.sh
    source ${XENADMIN_DIR}/mk/xenadmin-build.sh
    test_phase
    source ${XENADMIN_DIR}/mk/copy-build-output.sh
}

# Use this option if you're running on a Jenkins that is not the production Jenkins server
private_jenkins_build()
{
    source ${XENADMIN_DIR}/mk/xenadmin-build.sh
    test_phase
}

# Set the PRIVATE_BUILD_MODE variable in order to use the private build mode
# You can do this from the jenkins ui by putting "export PRIVATE_BUILD_MODE=1" above 
# the call for this script

if [ -z "${PRIVATE_BUILD_MODE+xxx}" ]; then
    production_jenkins_build
else
    echo "INFO:	Running private Jenkins build"
    private_jenkins_build
fi
unset PRIVATE_BUILD_MODE
