# ==============================================================================
# unfunctional - Level 4 Email Support Server - Deployment Makefile
# ==============================================================================
#
# EC2 FREE TIER SETUP INSTRUCTIONS
# =================================
# Run `make setup-instructions` to print these to the terminal.
#
# 1. Launch an EC2 instance in the AWS Console:
#      - AMI: Amazon Linux 2023 (free tier eligible)
#      - Instance type: t2.micro (free tier)
#      - Create or select a key pair (.pem file), save it locally
#      - Network: assign a public IP (enabled by default in default VPC)
#
# 2. Security Group inbound rules:
#      - TCP 22   (SSH)          - your IP only
#      - TCP 25   (SMTP inbound) - 0.0.0.0/0  (so anyone can email Rodney)
#      - TCP 3000 (HTTP API)     - 0.0.0.0/0  (game client validates codes)
#      NOTE: AWS blocks outbound port 25 by default on new accounts.
#            You must submit a "Request to remove email sending limitations"
#            form in AWS Support, or use an SMTP relay (SES, Mailgun, etc.)
#            for outbound replies. See SMTP_RELAY_* env vars.
#
# 3. Allocate an Elastic IP and associate it with the instance.
#    This gives you a stable public IP that persists across stop/start.
#
# 4. DNS setup for premiumdoorcodes.com:
#      - A record:  premiumdoorcodes.com       -> <Elastic IP>
#      - MX record: premiumdoorcodes.com       -> 10 premiumdoorcodes.com
#      (This tells the world that mail for @premiumdoorcodes.com goes to your server)
#
# 5. Fill in the variables below, then run:
#      make setup-ec2    (first-time server provisioning)
#      make deploy        (push code and restart)
#
# ==============================================================================

# ---- CONFIGURE THESE ----
EC2_HOST       ?= ec2-user@<YOUR_ELASTIC_IP>
KEY_FILE       ?= ~/.ssh/unfunctional-ec2.pem
REMOTE_DIR     ?= /home/ec2-user/email-support-server
# --------------------------

SSH_CMD        = ssh -i $(KEY_FILE) -o StrictHostKeyChecking=no $(EC2_HOST)
SCP_CMD        = scp -i $(KEY_FILE) -o StrictHostKeyChecking=no

.PHONY: help setup-instructions setup-ec2 deploy push start stop restart \
        logs status ssh env-push clean-remote

help: ## Show available targets
	@echo.
	@echo  unfunctional email-support-server
	@echo  ==================================
	@echo.
	@grep -E '^[a-zA-Z_-]+:.*##' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*## "}; {printf "  %-20s %s\n", $$1, $$2}'
	@echo.
	@echo  First time? Run: make setup-instructions

setup-instructions: ## Print EC2 free tier setup walkthrough
	@echo ================================================================
	@echo  EC2 FREE TIER SETUP - unfunctional email server
	@echo ================================================================
	@echo.
	@echo  1. Go to AWS Console ^> EC2 ^> Launch Instance
	@echo     - Name: unfunctional-email-server
	@echo     - AMI: Amazon Linux 2023 (free tier eligible)
	@echo     - Instance type: t2.micro (750 hrs/month free for 12 months)
	@echo     - Key pair: create new, download the .pem, chmod 400 it
	@echo     - Network: default VPC, auto-assign public IP = enabled
	@echo.
	@echo  2. Security Group (create new or edit default):
	@echo     Inbound rules:
	@echo       SSH    TCP 22    Your IP
	@echo       SMTP   TCP 25    0.0.0.0/0   (receive email from anyone)
	@echo       HTTP   TCP 3000  0.0.0.0/0   (game API)
	@echo.
	@echo  3. Elastic IP:
	@echo     EC2 ^> Elastic IPs ^> Allocate ^> Associate with instance
	@echo     (free as long as it is attached to a running instance)
	@echo.
	@echo  4. DNS for premiumdoorcodes.com:
	@echo     A  record:  premiumdoorcodes.com  ^-^>  ^<Elastic IP^>
	@echo     MX record:  premiumdoorcodes.com  ^-^>  10 premiumdoorcodes.com
	@echo.
	@echo  5. OUTBOUND EMAIL - IMPORTANT:
	@echo     AWS blocks port 25 outbound on new accounts. Two options:
	@echo     a) Submit a removal request in AWS Support Center
	@echo        (takes 1-2 business days to approve)
	@echo     b) Use an SMTP relay service. Set in .env:
	@echo          SMTP_RELAY_HOST=email-smtp.us-east-1.amazonaws.com
	@echo          SMTP_RELAY_PORT=587
	@echo          SMTP_RELAY_USER=^<SES SMTP user^>
	@echo          SMTP_RELAY_PASS=^<SES SMTP pass^>
	@echo.
	@echo  6. Configure this Makefile:
	@echo     EC2_HOST=ec2-user@^<your-elastic-ip^>
	@echo     KEY_FILE=~/.ssh/your-key.pem
	@echo.
	@echo  7. Run:
	@echo     make setup-ec2     (first-time server setup)
	@echo     make deploy        (push code and restart)
	@echo.
	@echo ================================================================

# --------------------------------------------------------------------------
# EC2 Instance Provisioning (run once on a fresh instance)
# --------------------------------------------------------------------------

setup-ec2: ## First-time EC2 setup: install Node.js 20, pm2, create dirs
	$(SSH_CMD) 'set -e && \
		echo ">>> Installing Node.js 20 LTS via dnf..." && \
		sudo dnf update -y && \
		sudo dnf install -y nodejs20 git && \
		node --version && npm --version && \
		echo ">>> Installing pm2 globally..." && \
		sudo npm install -g pm2 && \
		pm2 --version && \
		echo ">>> Creating app directory..." && \
		mkdir -p $(REMOTE_DIR) && \
		echo ">>> Configuring pm2 to start on boot..." && \
		pm2 startup systemd -u ec2-user --hp /home/ec2-user | tail -1 | sudo bash && \
		echo ">>> Done. Run: make deploy"'

# --------------------------------------------------------------------------
# Deployment
# --------------------------------------------------------------------------

push: ## Copy source files to EC2 (no restart)
	$(SCP_CMD) -r \
		index.js \
		package.json \
		$(EC2_HOST):$(REMOTE_DIR)/
	@echo ">>> Files pushed to $(EC2_HOST):$(REMOTE_DIR)"

env-push: ## Push .env file to EC2
	@if [ ! -f .env ]; then \
		echo "ERROR: No .env file found. Copy .env.example and fill it in."; \
		exit 1; \
	fi
	$(SCP_CMD) .env $(EC2_HOST):$(REMOTE_DIR)/.env
	@echo ">>> .env pushed"

deploy: push ## Full deploy: push code, install deps, restart
	$(SSH_CMD) 'set -e && \
		cd $(REMOTE_DIR) && \
		echo ">>> Installing dependencies..." && \
		npm install --production && \
		echo ">>> Restarting with pm2..." && \
		(pm2 describe unfunctional-email >/dev/null 2>&1 && \
			pm2 restart unfunctional-email) || \
			pm2 start index.js --name unfunctional-email && \
		pm2 save && \
		echo ">>> Deploy complete"'

# --------------------------------------------------------------------------
# Process management (pm2)
# --------------------------------------------------------------------------

start: ## Start the app via pm2
	$(SSH_CMD) 'cd $(REMOTE_DIR) && pm2 start index.js --name unfunctional-email && pm2 save'

stop: ## Stop the app via pm2
	$(SSH_CMD) 'pm2 stop unfunctional-email'

restart: ## Restart the app via pm2
	$(SSH_CMD) 'pm2 restart unfunctional-email && pm2 save'

logs: ## Tail pm2 logs (Ctrl+C to stop)
	$(SSH_CMD) 'pm2 logs unfunctional-email --lines 50'

status: ## Show pm2 process status
	$(SSH_CMD) 'pm2 status'

# --------------------------------------------------------------------------
# Convenience
# --------------------------------------------------------------------------

ssh: ## SSH into the EC2 instance
	$(SSH_CMD)

health: ## Hit the /health endpoint on the remote server
	@echo ">>> Checking health..."
	curl -s http://$(subst ec2-user@,,$(EC2_HOST)):3000/health | python3 -m json.tool 2>/dev/null || \
		curl -s http://$(subst ec2-user@,,$(EC2_HOST)):3000/health

test-smtp: ## Send a test email to rodney@premiumdoorcodes.com via the SMTP server (requires local smtp tools)
	@echo ">>> Sending test email to rodney@premiumdoorcodes.com..."
	@echo "Subject: test" | \
		curl --url "smtp://$(subst ec2-user@,,$(EC2_HOST)):25" \
		--mail-from "test@example.com" \
		--mail-rcpt "rodney@premiumdoorcodes.com" \
		-T - 2>&1 && echo ">>> Sent" || echo ">>> Failed (is port 25 open?)"

clean-remote: ## Remove app files from EC2 (leaves node/pm2 installed)
	$(SSH_CMD) 'pm2 delete unfunctional-email 2>/dev/null; rm -rf $(REMOTE_DIR)'
	@echo ">>> Cleaned remote"
