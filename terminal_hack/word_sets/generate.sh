#!/bin/bash

word_count=$1

for word_length in 4 6 8 10 12
do
	grep -Ex "[a-z]{${word_length}}" /usr/share/dict/american-english-large > temp
	
	available_word_count=$(cat temp | wc -l)
	if [ $available_word_count -lt $word_count ]; then
		echo "Source word file does not have enough words of length ${word_length}. Has ${available_word_count}. Requested ${word_count}"
		rm temp
		exit 1
	fi

	file_name=len_${word_length}.txt
	cat temp | shuf -n ${word_count} > ${file_name}
	rm temp
done

