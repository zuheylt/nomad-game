#!/bin/bash
# Start the Nomad backend. Set RELAYER_PRIVATE_KEY before running.
# Usage: RELAYER_PRIVATE_KEY=0x... ./run.sh

if [ -z "$RELAYER_PRIVATE_KEY" ]; then
  echo "ERROR: RELAYER_PRIVATE_KEY is not set."
  echo "Run as: RELAYER_PRIVATE_KEY=0x<your_key> ./run.sh"
  exit 1
fi

MVN=/home/temel/.m2/wrapper/dists/apache-maven-3.9.14/db91789b/bin/mvn

cd "$(dirname "$0")"
exec $MVN spring-boot:run
