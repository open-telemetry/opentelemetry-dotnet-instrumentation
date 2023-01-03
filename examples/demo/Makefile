.PHONY: run
run:
	mkdir -p log
	chmod 777 log
	docker-compose build
	docker-compose up -d service
	sleep 30 # wait for bootstrap
	docker-compose run client
	sleep 10 # wait for telemetry export

.PHONY: clean
clean:
	docker-compose down --remove-orphans
	rm -rf log

.PHONY: test
test:
	test -f "./log/traces.log"  || (echo "traces not exported"; exit 1)
	test -f "./log/metrics.log" || (echo "metrics not exported"; exit 1)
	test -f "./log/logs.log"    || (echo "logs not exported"; exit 1)

.PHONY: all
all: run test clean
