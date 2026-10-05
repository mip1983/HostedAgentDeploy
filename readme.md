# Repro of foundry hosted agent deployment issue.

If you run `aspire do diagnosics | clip` and paste the output somewhere, then search for `If targeting 'deploy-agent-ha`, you will
get:

```
If targeting 'deploy-agent-ha':
   Direct dependencies: deploy-prereq, provision-azure-bicep-resources, push-agent
   Total steps: 29
   Execution order:
     [0] process-parameters | validate-build-only-container-references (parallel)
     [1] build-prereq | deploy-prereq (parallel)
     [2] build-agent | build-apiservice | build-secondapiservice | build-webfrontend | validate-azure-login (parallel)
     [3] create-provisioning-context
     [4] provision-agent-identity | provision-agenttest-azure-mp123-acr | provision-foundry | provision-project-acr (parallel)
     [5] login-to-acr-agenttest-azure-mp123-acr | login-to-acr-project-acr | provision-agent-roles-foundry | 
provision-agenttest-azure-mp123 | provision-project (parallel)
     [6] push-prereq
     [7] push-agent | push-apiservice | push-secondapiservice | push-webfrontend (parallel)
     [8] provision-apiservice-containerapp | provision-secondapiservice-containerapp | provision-webfrontend-containerapp (parallel)
     [9] provision-azure-bicep-resources
     [10] deploy-agent-ha
```

This shows that despite the agent only referencing `api-service` yet if you were to run `aspire do deploy-agent-ha` it would still build and push `api-service`, `second-api-service`, and `web-frontend` even though they are not referenced by the agent.