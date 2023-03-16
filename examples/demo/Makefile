.PHONY: run
run:
	mkdir -p log
	chmod 777 log
	docker compose up -d --build

.PHONY: clean
clean:
	docker compose down --remove-orphans
	rm -rf log

.PHONY: wait
wait:
	until [ -f "./log/traces.log" ] && [ -f "./log/metrics.log" ] && [ -f "./log/logs.log" ]; do sleep 5; done

.PHONY: test
test: run wait clean
