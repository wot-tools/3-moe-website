class MarksController < ApplicationController

	def index
		@marks = Mark.all
	end

end
