import os
from pathlib import Path
import re
import shutil
import subprocess
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[1]
BASH = shutil.which("bash")
if os.name == "nt":
    git_bash = Path(r"C:\Program Files\Git\bin\bash.exe")
    if git_bash.exists():
        BASH = str(git_bash)


def workflow_script(file_name, step_name):
    workflow = (ROOT / ".github" / "workflows" / file_name).read_text()
    step = workflow.split(f"- name: {step_name}\n", 1)[1].split("- name:", 1)[0]
    match = re.search(r"(?m)^([ ]*)run: \|\n", step)
    return textwrap.dedent(step[match.end():])


STUB = r"""
exec 3>&1
export ORG=XpiritCommunityEvents TEMPLATE_REPOSITORY=XpiritCommunityEvents/HOLAgentFramework
export GITHUB_HANDLE=old-handle ATTENDEE_USER_ID=123
property_file=$(mktemp)
trap 'rm -f "$property_file"' EXIT
gh() {
  printf 'CALL %s\n' "$*" >&3
  case "$*" in
    *properties/schema*)
      [[ "$MODE" == unsafe-schema ]] && { printf 'false\n'; return 0; }
      printf 'true\n' ;;
    *'/orgs/XpiritCommunityEvents/repos?'*) printf 'attendee-workshopAF-old-handle\n' ;;
    *'--method PATCH'*)
      payload=$(cat)
      [[ "$payload" == '{"properties":[{"property_name":"holagentframework_attendee_user_id","value":"123"}]}' ]] || return 99
      [[ "$MODE" == write-failure ]] && return 1
      printf '123\n' > "$property_file" ;;
    *properties/values*)
      [[ "$MODE" == property-error ]] && return 1
      [[ "$MODE" == invalid ]] && { printf 'not-an-id\n'; return 0; }
      [[ "$MODE" == conflict ]] && { printf '456\n'; return 0; }
      [[ "$MODE" == missing ]] && return 0
      [[ "$MODE" == readback-error ]] && return 0
      if [[ "$MODE" != provision || -s "$property_file" ]]; then printf '123\n'; fi ;;
    'api user/123 --jq .login')
      [[ "$MODE" == user-error ]] && return 1
      printf 'new-handle\n' ;;
    *'--method DELETE'*)
      [[ "$MODE" == remove-error ]] && { printf 'HTTP/2 403 Forbidden\n'; return 1; }
      printf 'HTTP/2 204 No Content\n' ;;
    'repo delete '*)
      [[ "$MODE" == delete-error ]] && return 1
      return 0 ;;
    'repo view '*) return 0 ;;
    'api repos/XpiritCommunityEvents/attendee-workshopAF-old-handle --jq '*)
      [[ "$MODE" == unrelated ]] && { printf 'other/template\n'; return 0; }
      printf '%s\n' "$TEMPLATE_REPOSITORY" ;;
    *) echo "Unexpected gh call: $*" >&2; return 99 ;;
  esac
}
"""


@unittest.skipUnless(BASH, "Bash is required to test workflow scripts")
class ParticipantWorkflowsTest(unittest.TestCase):
    def run_script(self, script, mode):
        return subprocess.run(
            [BASH, "--noprofile", "--norc", "-s"],
            input="set -eo pipefail\n" + STUB + script,
            text=True,
            capture_output=True,
            env={**os.environ, "MODE": mode},
            timeout=30,
        )

    def test_cleanup_resolves_identity_by_id(self):
        script = workflow_script(
            "deprovision-participant.yml",
            "Remove workshop attendees and delete template copies",
        )
        for mode in ("normal", "missing", "invalid", "property-error", "user-error",
                     "unsafe-schema", "unrelated", "remove-error", "delete-error"):
            with self.subTest(mode=mode):
                result = self.run_script(script, mode)
                output = result.stdout + result.stderr
                self.assertEqual(result.returncode, 0 if mode in ("normal", "unrelated") else 1, output)
                if mode == "normal":
                    self.assertIn("/memberships/new-handle", output)
                    self.assertIn("CALL repo delete", output)
                elif mode != "delete-error":
                    self.assertNotIn("CALL repo delete", output)
                if mode not in ("normal", "remove-error", "delete-error"):
                    self.assertNotIn("/memberships/", output)
                self.assertNotIn("/memberships/old-handle", output)

    def test_provision_stores_string_id_and_checks_readback(self):
        script = workflow_script("provision-participant.yml", "Create Repository")
        for mode in ("provision", "conflict", "write-failure", "readback-error", "unrelated"):
            with self.subTest(mode=mode):
                result = self.run_script(script, mode)
                self.assertEqual(result.returncode, 0 if mode == "provision" else 1,
                                 result.stdout + result.stderr)
                if mode in ("conflict", "unrelated"):
                    self.assertNotIn("CALL api --method PATCH", result.stdout)


if __name__ == "__main__":
    unittest.main()
